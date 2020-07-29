using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using LiteNetLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkServer : IDisposable {
        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<PlayerId, NetPeer> _peers;
        private JsonSerializer _serializer;
        private IDisposable _poll;
        private IDisposable _connectReply;

        public NetworkServer(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayersActions playerActions
        ) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<PlayerId, NetPeer>();
            _serializer = JsonSerializer.Create(JsonConvert.DefaultSettings());
            _serializer.TypeNameHandling = TypeNameHandling.All;

            _listener.ConnectionRequestEvent += request => {
                if (_server.ConnectedPeersCount < 10) {
                    request.AcceptIfKey("BanchouConnectionKey");
                } else {
                    request.Reject();
                }
            };

            _listener.PeerConnectedEvent += peer => {
                var playerId = PlayerId.Create();
                _peers[playerId] = peer;
                dispatch(playerActions.AddNetworkPlayer(playerId, peer.EndPoint, peer.Id));
            };
            _instances.Add(this);

            _connectReply = observeState
                .DistinctUntilChanged(state => state.GetPlayers())
                .Pairwise()
                .Subscribe(delta => {
                    var newPlayers = delta.Current.GetPlayerIds().Except(delta.Previous.GetPlayerIds());

                    foreach (var playerId in newPlayers) {
                        NetPeer peer;
                        if (_peers.TryGetValue(playerId, out peer)) {
                            var memoryStream = new MemoryStream();
                            using (var writer = new BsonWriter(memoryStream)) {
                                _serializer.Serialize(writer, new PlayerConnected {
                                    PlayerId = playerId,
                                    GameState = delta.Current
                                });
                            }
                            peer.Send(memoryStream.ToArray(), DeliveryMethod.ReliableSequenced);
                        }
                    }
                });
        }

        public void SyncPawn(SyncPawn syncPawn) {
            byte[] syncData = null;
            using (var writer = new BsonWriter(new MemoryStream())) {
                _serializer.Serialize(writer, new Envelope {
                    PayloadType = PayloadType.SyncPawn,
                    Payload = syncPawn
                });
            }

            if (syncData != null) {
                foreach (var peer in _peers.Values) {
                    peer.Send(syncData, DeliveryMethod.Sequenced);
                }
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
        }

        #region Redux Middleware
        private static List<NetworkServer> _instances = new List<NetworkServer>();
        public static Middleware<TState> Install<TState>() {
            var serializer = JsonSerializer.Create(JsonConvert.DefaultSettings());
            serializer.TypeNameHandling = TypeNameHandling.All;

            return store => next => action => {
                byte[] actionData = null;
                for (int i = 0; i < _instances.Count; i++) {
                    // Send the action to all peers
                    foreach (var peer in _instances[i]._peers.Values) {
                        // Serialize the action into a BSON bytestring if we haven't already
                        if (actionData == null) {
                            var memoryStream = new MemoryStream();
                            using (var writer = new BsonWriter(memoryStream)) {
                                serializer.Serialize(
                                    writer,
                                    new Envelope {
                                        PayloadType = PayloadType.Action,
                                        Payload = action
                                    }
                                );
                            }
                            actionData = memoryStream.ToArray();
                        }
                        peer.Send(actionData, DeliveryMethod.ReliableUnordered);
                    }
                }
                return next(action);
            };
        }
        #endregion
    }
}