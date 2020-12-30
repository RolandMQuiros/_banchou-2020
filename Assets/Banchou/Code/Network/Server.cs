using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network.Message;

using Stopwatch = System.Diagnostics.Stopwatch;

#pragma warning disable 0618

namespace Banchou.Network {
    public class NetworkServer : IDisposable {
        private Guid _networkId;
        private Dispatcher _dispatch;
        private NetworkActions _networkActions;
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<Guid, NetPeer> _peers;
        private CompositeDisposable _subscriptions = new CompositeDisposable();

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        public NetworkServer(
            Guid networkId,
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayerInputStreams playerInput,
            JsonSerializer jsonSerializer,
            MessagePackSerializerOptions messagePackOptions
        ) {
            _networkId = networkId;
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<Guid, NetPeer>();
            _messagePackOptions = messagePackOptions;
            _dispatch = dispatch;
            _networkActions = networkActions;

            var clients = new Dictionary<IPEndPoint, ConnectClient>();

            long When(float ping) {
                return _stopwatch.ElapsedTicks - TimeSpan.FromMilliseconds(ping / 2).Ticks;
            }

            _listener.ConnectionRequestEvent += request => {
                Debug.Log($"Connection request from {request.RemoteEndPoint}");

                var connectData = MessagePackSerializer.Deserialize<ConnectClient>(request.Data.GetRemainingBytes(), _messagePackOptions);

                if (_server.ConnectedPeersCount < 10 && connectData.ConnectionKey == "BanchouConnectionKey") {
                    request.Accept();
                    clients[request.RemoteEndPoint] = connectData;
                    Debug.Log($"Accepted connection from {request.RemoteEndPoint}");
                } else {
                    request.Reject();
                }
            };

            _listener.PeerConnectedEvent += peer => {
                Debug.Log($"Setting up client connection from {peer.EndPoint}");

                // Generate a new network ID
                var newNetworkId = Guid.NewGuid();
                _dispatch(networkActions.ConnectedToClient(newNetworkId));

                _peers[newNetworkId] = peer;

                // Sync the client's state
                var gameStateStream = new MemoryStream();
                using (var writer = new BsonWriter(gameStateStream)) {
                    jsonSerializer.Serialize(writer, getState());
                }

                var syncClientMessage = Envelope.CreateMessage(
                    PayloadType.SyncClient,
                    new SyncClient {
                        ClientNetworkId = newNetworkId,
                        GameStateBytes = gameStateStream.ToArray(),
                        ClientTime = clients[peer.EndPoint].ClientConnectionTime,
                        ServerTime = When(peer.Ping)
                    },
                    _messagePackOptions
                );

                peer.Send(syncClientMessage, DeliveryMethod.ReliableOrdered);
            };

            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Open envelope
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), _messagePackOptions);

                // Deserialize payload
                switch (envelope.PayloadType) {
                    case PayloadType.ServerTimeRequest: {
                        var request = MessagePackSerializer.Deserialize<ServerTimeRequest>(envelope.Payload, _messagePackOptions);
                        var response = Envelope.CreateMessage(
                            PayloadType.ServerTimeResponse,
                            new ServerTimeResponse {
                                ClientTime = request.ClientTime,
                                ServerTime = When(fromPeer.Ping)
                            },
                            _messagePackOptions
                        );
                        fromPeer.Send(response, DeliveryMethod.Unreliable);
                    } break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(envelope.Payload, _messagePackOptions);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, playerCommand.When);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(envelope.Payload, _messagePackOptions);
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, playerMove.When);
                    } break;
                }

                dataReader.Recycle();
            };

            // Send input to all peers, provided they're not the source
            _subscriptions.Add(
                playerInput.ObserveMove()
                    .DistinctUntilChanged()
                    .Pairwise()
                    .CatchIgnoreLog()
                    .Subscribe(pair => {
                        foreach (var peer in _peers) {
                            var playerNetworkId = getState().GetPlayerNetworkId(pair.Current.PlayerId);
                            if (peer.Key != playerNetworkId) {
                                var currentMessage = Envelope.CreateMessage(
                                    PayloadType.PlayerMove,
                                    new PlayerMove {
                                        PlayerId = pair.Current.PlayerId,
                                        Direction = pair.Current.Move,
                                        When = pair.Current.When
                                    },
                                    _messagePackOptions
                                );

                                peer.Value.Send(currentMessage, DeliveryMethod.ReliableUnordered);
                            }
                        }
                    })
            );

            _subscriptions.Add(
                playerInput.ObserveCommand()
                    .CatchIgnoreLog()
                    .Subscribe(command => {
                        foreach (var peer in _peers) {
                            var playerNetworkId = getState().GetPlayerNetworkId(command.PlayerId);
                            // Don't send clients their own inputs
                            if (peer.Key != playerNetworkId) {
                                var message = Envelope.CreateMessage(
                                    PayloadType.PlayerCommand,
                                    new PlayerCommand {
                                        PlayerId = command.PlayerId,
                                        Command = command.Command,
                                        When = command.When
                                    },
                                    _messagePackOptions
                                );
                                peer.Value.Send(message, DeliveryMethod.ReliableOrdered);
                            }
                        }
                    })
            );

            _instances[networkId] = this;
            Debug.Log($"Network server constructed {Stopwatch.IsHighResolution}");
        }

        public void SyncPawn(SyncPawn syncPawn) {
            var syncPawnMessage = Envelope.CreateMessage(
                PayloadType.SyncPawn,
                syncPawn,
                _messagePackOptions
            );

            foreach (var peer in _peers.Values) {
                peer.Send(syncPawnMessage, DeliveryMethod.Sequenced);
            }
        }

        public float GetTime() {
            return (float)_stopwatch.Elapsed.TotalSeconds;
        }

        public NetworkServer Start<T>(IObservable<T> pollInterval) {
            _server.Start(9050);
            Debug.Log($"Server started on port {_server.LocalPort}");
            _subscriptions.Add(
                pollInterval
                    .CatchIgnoreLog()
                    .Subscribe(_ => { _server.PollEvents(); })
            );
            return this;
        }

        public void Dispose() {
            Debug.Log("Server shutting down");
            _server.Stop();
            Debug.Log("Server stopped");

            _subscriptions.Dispose();
            _instances.Remove(_networkId);
        }

        #region Redux Middleware
        private static Dictionary<Guid, NetworkServer> _instances = new Dictionary<Guid, NetworkServer>();
        public static Middleware<TState> Install<TState>(JsonSerializer jsonSerializer, MessagePackSerializerOptions messagePackOptions) {
            return store => next => action => {
                NetworkServer server;
                if (store.GetState() is GameState state && state.IsServer() && _instances.TryGetValue(state.GetNetworkId(), out server)) {
                    byte[] actionBytes = null;
                    // Send the action to all peers
                    foreach (var peer in server._peers.Values) {
                        // If the envelope hasn't been built, build it
                        if (actionBytes == null) {
                            // Convert action to BSON
                            var actionStream = new MemoryStream();
                            using (var writer = new BsonWriter(actionStream)) {
                                jsonSerializer.Serialize(writer, action);
                            }

                            // Pack into envelope and serialize to bytestream
                            actionBytes = Envelope.CreateMessage(
                                PayloadType.ReduxAction,
                                new ReduxAction {
                                    ActionBytes = actionStream.ToArray()
                                },
                                messagePackOptions
                            );
                        }

                        // Send bytestream to peer
                        peer.Send(actionBytes, DeliveryMethod.ReliableUnordered);
                    }
                }
                return next(action);
            };
        }
        #endregion
    }
}