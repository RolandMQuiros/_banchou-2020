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
        public IObservable<InputUnit> ObserveRemoteInput { get; private set; }

        private Guid _networkId;
        private Dispatcher _dispatch;
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<Guid, NetPeer> _peers;
        private CompositeDisposable _subscriptions;

        public NetworkServer(
            Guid networkId,
            IObservable<GameState> onStateUpdate,
            GetState getState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            IObservable<InputUnit> observeLocalInput,
            JsonSerializer jsonSerializer,
            MessagePackSerializerOptions messagePackOptions
        ) {
            _networkId = networkId;
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<Guid, NetPeer>();
            _messagePackOptions = messagePackOptions;
            _dispatch = dispatch;

            var remoteInput = new Subject<InputUnit>();
            var clients = new Dictionary<IPEndPoint, ConnectClient>();

            ObserveRemoteInput = remoteInput;

            float When(float ping) {
                return Snapping.Snap(GetTime() - (ping / 1000f), Time.fixedUnscaledDeltaTime);
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
                dataReader.Recycle();

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
                    case PayloadType.PlayerInput: {
                        var inputUnit = MessagePackSerializer.Deserialize<InputUnit>(envelope.Payload, _messagePackOptions);
                        remoteInput.OnNext(inputUnit);
                    } break;
                }
            };

            // Send input to all peers, provided they're not the source
            _subscriptions = new CompositeDisposable(
                onStateUpdate
                    .Select(state => state.GetSimulatedLatency())
                    .DistinctUntilChanged()
                    .CatchIgnoreLog()
                    .Subscribe(latency => {
                        _server.SimulateLatency = latency.Min != 0 || latency.Max != 0;
                        _server.SimulationMinLatency = latency.Min;
                        _server.SimulationMaxLatency = latency.Max;
                    }),
                observeLocalInput
                    .ObserveCommands()
                    .Merge(observeLocalInput.ObserveMoves())
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        foreach (var peer in _peers) {
                            var playerNetworkId = getState().GetPlayerNetworkId(unit.PlayerId);
                            if (peer.Key != playerNetworkId) {
                                var currentMessage = Envelope.CreateMessage(
                                    PayloadType.PlayerInput,
                                    unit,
                                    _messagePackOptions
                                );

                                peer.Value.Send(currentMessage, DeliveryMethod.ReliableUnordered);
                            }
                        }
                    })
            );

            _instances[networkId] = this;
            Debug.Log($"Network server constructed");
        }

        public float GetTime() {
            return Snapping.Snap(Time.fixedUnscaledTime, Time.fixedUnscaledDeltaTime);
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
        private static Stopwatch _serializationPerf = new Stopwatch();

        public static Middleware<TState> Install<TState>(JsonSerializer jsonSerializer, MessagePackSerializerOptions messagePackOptions) {
            return store => next => action => {
                NetworkServer server;
                var state = store.GetState() as GameState;
                var isServer = state != null && state.IsServer();
                var isLocalAction = Attribute.GetCustomAttribute(action.GetType(), typeof(LocalActionAttribute)) != null;

                if (isServer && !isLocalAction && _instances.TryGetValue(state.GetNetworkId(), out server)) {
                    byte[] actionBytes = null;
                    // Send the action to all peers
                    foreach (var peer in server._peers.Values) {
                        // If the envelope hasn't been built, build it
                        if (actionBytes == null) {
                            _serializationPerf.Restart();

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

                            _serializationPerf.Stop();
                            UnityEngine.Debug.Log($"{action.GetType().Name} serialized to {actionBytes.Length} bytes in {_serializationPerf.ElapsedMilliseconds} ms");
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