using System;
using System.Net;
using System.IO;

using LiteNetLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UnityEngine;
using UniRx;

using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkClient : IDisposable {
        private EventBasedNetListener _listener;
        private NetManager _client;
        private NetPeer _peer;
        private IDisposable _poll;

        public NetworkClient(
            Dispatcher dispatch,
            PlayerInputStreams playerInput,
            Action<SyncPawn> pullPawnSync
        ) {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            var serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.All;

            var buffer = new MemoryStream();

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);

                // Extract payload type
                var payloadType = (PayloadType)dataReader.GetByte();

                // Deserialize payload
                buffer.Write(dataReader.RawData, 1, dataReader.RawDataSize);
                object payload;
                using (var reader = new BsonReader(buffer)) {
                    payload = serializer.Deserialize(reader);
                }

                switch (payloadType) {
                    case PayloadType.Action:
                        dispatch(payload);
                    break;
                    case PayloadType.SyncPawn:
                        pullPawnSync((SyncPawn)payload);
                    break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = (PlayerCommand)payload;
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = (PlayerMove)payload;
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, when);
                    } break;
                }

                buffer.SetLength(0);
                dataReader.Recycle();
            };
        }

        public NetworkClient Start<T>(IPEndPoint host, IObservable<T> pollInterval) {
            _client.Start();
            _peer = _client.Connect(host, "BanchouConnectionKey");

            _poll = pollInterval
                .Subscribe(_ => {
                    _client.PollEvents();
                });

            return this;
        }

        public void Dispose() {
            _client.Stop();
            _poll.Dispose();
        }
    }
}