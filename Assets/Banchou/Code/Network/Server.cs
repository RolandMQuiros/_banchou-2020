﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using LiteNetLib;
using MessagePack;
using MessagePack.Resolvers;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkServer : IDisposable {
        private static readonly MessagePackSerializerOptions _messagePackOptions = MessagePackSerializerOptions
            .Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<PlayerId, NetPeer> _peers;
        private IDisposable _poll;
        private IDisposable _connectReply;

        public NetworkServer(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayersActions playerActions,
            PlayerInputStreams playerInput
        ) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<PlayerId, NetPeer>();

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
                var playerId = PlayerId.Create();
                _peers[playerId] = peer;
                dispatch(playerActions.AddNetworkPlayer(playerId, peer.EndPoint.Address.ToString(), peer.Id));
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

            _connectReply = observeState
                .DistinctUntilChanged(state => state.GetPlayers())
                .Pairwise()
                .Subscribe(delta => {
                    var newPlayers = delta.Current.GetPlayerIds().Except(delta.Previous.GetPlayerIds());

                    foreach (var playerId in newPlayers) {
                        NetPeer peer;
                        if (_peers.TryGetValue(playerId, out peer)) {
                            var memoryStream = new MemoryStream();
                            memoryStream.WriteByte((byte)PayloadType.SyncClient);

                            MessagePackSerializer.Serialize(
                                memoryStream,
                                new SyncClient {
                                    PlayerId = playerId,
                                    GameState = delta.Current
                                }
                            );

                            peer.Send(memoryStream.ToArray(), DeliveryMethod.ReliableSequenced);
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
            return this;
        }

        public void Dispose() {
            _server.Stop();
            _poll.Dispose();
            _connectReply.Dispose();
            _instances.Remove(this);
            Debug.Log("Server stopped");
        }

        #region Redux Middleware
        private static List<NetworkServer> _instances = new List<NetworkServer>();
        public static Middleware<TState> Install<TState>() {
            JsonSerializer serializer = null;

            return store => next => action => {
                MemoryStream memoryStream = null;
                serializer = serializer ?? new JsonSerializer();

                for (int i = 0; i < _instances.Count; i++) {
                    // Send the action to all peers
                    foreach (var peer in _instances[i]._peers.Values) {
                        if (memoryStream == null) {
                            memoryStream = new MemoryStream();

                            var actionStream = new MemoryStream();
                            using (var writer = new BsonWriter(actionStream)) {
                                serializer.Serialize(writer, action);
                            }

                            MessagePackSerializer.Serialize(
                                memoryStream,
                                new Envelope {
                                    PayloadType = PayloadType.ReduxAction,
                                    Payload = actionStream.ToArray()
                                },
                                _messagePackOptions
                            );
                        }
                        peer.Send(memoryStream.ToArray(), DeliveryMethod.ReliableUnordered);
                    }
                }
                return next(action);
            };
        }
        #endregion
    }
}