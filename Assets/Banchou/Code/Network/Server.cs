﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network.Message;

#pragma warning disable 0618

namespace Banchou.Network {
    public class NetworkServer : IDisposable {
        private Dispatcher _dispatch;
        private NetworkActions _networkActions;
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<PlayerId, NetPeer> _peers;
        private IDisposable _poll;
        private IDisposable _connectReply;

        public NetworkServer(
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayersActions playerActions,
            PlayerInputStreams playerInput,
            JsonSerializer jsonSerializer,
            MessagePackSerializerOptions messagePackOptions
        ) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<PlayerId, NetPeer>();
            _messagePackOptions = messagePackOptions;
            _dispatch = dispatch;
            _networkActions = networkActions;

            _listener.ConnectionRequestEvent += request => {
                Debug.Log($"Connection request from {request.RemoteEndPoint.ToString()}");
                if (_server.ConnectedPeersCount < 10) {
                    request.AcceptIfKey("BanchouConnectionKey");
                    Debug.Log($"Accepted connection from {request.RemoteEndPoint.ToString()}");
                } else {
                    request.Reject();
                }
            };

            _listener.PeerConnectedEvent += peer => {
                // Add the peer as a player before sync'ing the client's state
                var playerId = getState().CreatePlayerId();
                _peers[playerId] = peer;
                _dispatch(playerActions.AddPlayer(playerId, null, peer.EndPoint, peer.Id));
            };

            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);

                // Open envelope
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), _messagePackOptions);

                // Deserialize payload
                switch (envelope.PayloadType) {
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(envelope.Payload);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(envelope.Payload);
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, when);
                    } break;
                }

                dataReader.Recycle();
            };

            // Sync new players' states
            _connectReply = observeState
                .DistinctUntilChanged(state => state.GetPlayers())
                .Pairwise()
                .Subscribe(delta => {
                    var newPlayers = delta.Current.GetPlayerIds().Except(delta.Previous.GetPlayerIds());

                    foreach (var playerId in newPlayers) {
                        NetPeer peer;
                        if (_peers.TryGetValue(playerId, out peer)) {
                            var memoryStream = new MemoryStream((byte)PayloadType.SyncClient);

                            var gameStateStream = new MemoryStream();
                            using (var writer = new BsonWriter(gameStateStream)) {
                                jsonSerializer.Serialize(writer, delta.Current);
                            }

                            MessagePackSerializer.Serialize(
                                memoryStream,
                                new Envelope {
                                    PayloadType = PayloadType.SyncClient,
                                    Payload = MessagePackSerializer.Serialize(
                                        new SyncClient {
                                            PlayerId = playerId,
                                            GameStateBytes = gameStateStream.ToArray()
                                        },
                                        _messagePackOptions
                                    )
                                },
                                _messagePackOptions
                            );

                            peer.Send(memoryStream.ToArray(), DeliveryMethod.ReliableOrdered);
                        }
                    }
                });

            _instances.Add(this);
            Debug.Log("Server constructed");
        }

        public void SyncPawn(SyncPawn syncPawn) {
            var memoryStream = new MemoryStream();
            memoryStream.WriteByte((byte)PayloadType.SyncPawn);
            MessagePackSerializer.Serialize(
                memoryStream,
                syncPawn
            );

            foreach (var peer in _peers.Values) {
                peer.Send(memoryStream.ToArray(), DeliveryMethod.Sequenced);
            }
        }

        public NetworkServer Start<T>(IObservable<T> pollInterval) {
            _server.Start(9050);
            Debug.Log($"Server started on port {_server.LocalPort}");
            _poll = pollInterval
                .Subscribe(_ => {
                    _server.PollEvents();
                });
            // _dispatch(_networkActions.Started(_server.Peer))
            return this;
        }

        public void Dispose() {
            Debug.Log("Server shutting down");
            _server.Stop();
            Debug.Log("Server stopped");

            _poll.Dispose();
            _connectReply.Dispose();
            _instances.Remove(this);
        }

        #region Redux Middleware
        private static List<NetworkServer> _instances = new List<NetworkServer>();
        public static Middleware<TState> Install<TState>(JsonSerializer jsonSerializer, MessagePackSerializerOptions messagePackOptions) {
            return store => next => action => {
                byte[] actionBytes = null;
                for (int i = 0; i < _instances.Count; i++) {
                    // Send the action to all peers
                    foreach (var peer in _instances[i]._peers.Values) {
                        // If the envelope hasn't been built, build it
                        if (actionBytes == null) {
                            // Convert action to BSON
                            var actionStream = new MemoryStream();
                            using (var writer = new BsonWriter(actionStream)) {
                                jsonSerializer.Serialize(writer, action);
                            }

                            // Pack into envelope and serialize to bytestream
                            actionBytes = MessagePackSerializer.Serialize(
                                new Envelope {
                                    PayloadType = PayloadType.ReduxAction,
                                    Payload = actionStream.ToArray()
                                },
                                messagePackOptions
                            );
                        }

                        // Send bytestream to peer
                        peer.Send(actionBytes, DeliveryMethod.Sequenced);
                    }
                }
                return next(action);
            };
        }
        #endregion
    }
}