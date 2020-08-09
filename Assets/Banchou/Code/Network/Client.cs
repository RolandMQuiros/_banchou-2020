using System;
using System.IO;
using System.Net;

using LiteNetLib;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UnityEngine;
using UniRx;

using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkClient : IDisposable {
        private static readonly MessagePackSerializerOptions _messagePackOptions = MessagePackSerializerOptions
            .Standard
            .WithCompression(MessagePackCompression.Lz4BlockArray);

        private EventBasedNetListener _listener;
        private NetManager _client;
        private NetPeer _peer;
        private IDisposable _poll;

        public NetworkClient(
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayerInputStreams playerInput,
            Action<SyncPawn> pullPawnSync
        ) {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            var jsonSerializer = new JsonSerializer();

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), _messagePackOptions);

                // Using the type flag, check what we need to deserialize message into
                switch (envelope.PayloadType) {
                    case PayloadType.SyncClient:
                        var syncClient = MessagePackSerializer.Deserialize<SyncClient>(envelope.Payload, _messagePackOptions);
                        dispatch(networkActions.SyncGameState(syncClient.GameState));
                    break;
                    case PayloadType.ReduxAction: {
                        var bsonStream = new MemoryStream();
                        using (var reader = new BsonReader(bsonStream)) {
                            var action = jsonSerializer.Deserialize(reader);
                            dispatch(action);
                        }
                    } break;
                    case PayloadType.SyncPawn:
                        var pawnSync = MessagePackSerializer.Deserialize<SyncPawn>(envelope.Payload, _messagePackOptions);
                        pullPawnSync(pawnSync);
                    break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(envelope.Payload, _messagePackOptions);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(envelope.Payload, _messagePackOptions);
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, when);
                    } break;
                }

                dataReader.Recycle();
            };
        }

        public NetworkClient Start<T>(IPEndPoint host, IObservable<T> pollInterval) {
            _client.Start();
            _peer = _client.Connect("localhost", 9050, "BanchouConnectionKey");

            _poll = pollInterval
                .Subscribe(_ => { _client.PollEvents(); });

            return this;
        }

        public void Dispose() {
            _client.Stop();
            _poll.Dispose();
        }
    }
}