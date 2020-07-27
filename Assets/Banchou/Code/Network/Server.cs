using System;
using System.Linq;
using System.Collections.Generic;

using LiteNetLib;
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
        private IDisposable _poll;
        private IDisposable _connectReply;

        public NetworkServer(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions
        ) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<PlayerId, NetPeer>();

            _listener.ConnectionRequestEvent += request => {
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
                            peer.Send(
                                new PlayerConnected {
                                    PlayerId = playerId,
                                    GameState = delta.Current
                                }.ToByteArray(),
                                DeliveryMethod.ReliableSequenced
                            );
                        }
                    }
                });
        }

        public void SyncPawn(SyncPawn syncPawn) {
            var message = new Envelope {
                PayloadType = PayloadType.SyncPawn,
                Payload = syncPawn
            }.ToByteArray();

            foreach (var peer in _peers.Values) {
                peer.Send(message, DeliveryMethod.Sequenced);
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
            return store => next => action => {
                byte[] actionData = null;
                for (int i = 0; i < _instances.Count; i++) {
                    // Send the action to all peers
                    foreach (var peer in _instances[i]._peers.Values) {
                        // Serialize the action into a BSON bytestring if we haven't already
                        if (actionData == null) {
                            actionData = new Envelope {
                                PayloadType = PayloadType.Action,
                                Payload = action
                            }.ToByteArray();
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