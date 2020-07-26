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
        private IPEndPoint _host;
        private IDisposable _poll;

        public NetworkClient(
            IPEndPoint host,
            Dispatcher dispatch,
            PushPawnSync pushPawnSync,
            PlayerInputStreams playerInput
        ) {
            _host = host;
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new Vec2Conv());
            serializer.Converters.Add(new Vec3Conv());
            serializer.Converters.Add(new Vec4Conv());
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
                        pushPawnSync((SyncPawn)payload);
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

        public void Start<T>(IObservable<T> pollInterval) {
            _client.Start();
            _peer = _client.Connect(_host, "BanchouConnectionKey");

            _poll = pollInterval
                .Subscribe(_ => {
                    _client.PollEvents();
                });
        }

        public void Dispose() {
            _client.Stop();
            _poll.Dispose();
        }
    }
}