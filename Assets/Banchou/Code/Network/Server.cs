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
using Banchou.Pawn;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkServer : IDisposable {
        private EventBasedNetListener _listener;
        private NetManager _server;
        private Dictionary<PlayerId, NetPeer> _peers;
        private JsonSerializer _serializer;
        private IDisposable _poll;
        private IDisposable _connectionReply;

        public NetworkServer(
            Dispatcher dispatch,
            PlayerActions playerActions
        ) {
            _listener = new EventBasedNetListener();
            _server = new NetManager(_listener);
            _peers = new Dictionary<PlayerId, NetPeer>();
            _serializer = new JsonSerializer();

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

                var memoryStream = new MemoryStream();
                using (var writer = new BsonWriter(memoryStream)) {
                    _serializer.Serialize(writer, new PlayerConnected { PlayerId = playerId });
                }
                peer.Send(memoryStream.ToArray(), DeliveryMethod.ReliableSequenced);
            };
            _instances.Add(this);
        }

        public void SyncPawn(PawnId pawnId, Vector3 position, Quaternion rotation) {
            byte[] syncData = null;
            using (var writer = new BsonWriter(new MemoryStream())) {
                _serializer.Serialize(writer, new Envelope {
                    PayloadType = PayloadType.SyncPawn,
                    Payload = new SyncPawn {
                        PawnId = pawnId,
                        Position = position,
                        Rotation = rotation
                    }
                });
            }

            if (syncData != null) {
                foreach (var peer in _peers.Values) {
                    peer.Send(syncData, DeliveryMethod.Sequenced);
                }
            }
        }

        public void Start<T>(IObservable<T> pollInterval) {
            _server.Start(9050);
            _poll = pollInterval
                .Subscribe(_ => {
                    _server.PollEvents();
                });
        }

        public void Dispose() {
            _server.Stop();
            _connectionReply.Dispose();
            _poll.Dispose();
            _instances.Remove(this);
        }

        #region Redux Middleware
        private static List<NetworkServer> _instances = new List<NetworkServer>();
        public static Middleware<TState> Install<TState>() {
            MemoryStream memoryStream = new MemoryStream();
            var serializer = new JsonSerializer();

            return store => next => action => {
                byte[] actionData = null;
                for (int i = 0; i < _instances.Count; i++) {
                    // Serialize the action into a BSON bytestring if we haven't already
                    if (actionData == null) {
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

                    // Send the action to all peers
                    foreach (var peer in _instances[i]._peers.Values) {
                        peer.Send(actionData, DeliveryMethod.ReliableUnordered);
                    }
                }
                return next(action);
            };
        }
        #endregion
    }
}