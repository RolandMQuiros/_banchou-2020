using System;
using System.IO;
using System.Linq;
using System.Net;

using LiteNetLib;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Redux;
using UnityEngine;
using UniRx;

using Banchou.Player;
using Banchou.Network.Message;

#pragma warning disable 0618

namespace Banchou.Network {
    public class NetworkClient : IDisposable {
        private Guid _networkId;
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _client;
        private NetPeer _peer;

        private CompositeDisposable _subscriptions = new CompositeDisposable();

        public NetworkClient(
            Guid networkId,
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayerInputStreams playerInput,
            Action<SyncPawn> pullPawnSync,
            JsonSerializer jsonSerializer,
            MessagePackSerializerOptions messagePackOptions
        ) {
            _networkId = networkId;
            _messagePackOptions = messagePackOptions;
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), messagePackOptions);

                // Using the type flag, check what we need to deserialize message into
                switch (envelope.PayloadType) {
                    case PayloadType.SyncClient: {
                        var syncClient = MessagePackSerializer.Deserialize<SyncClient>(envelope.Payload, messagePackOptions);
                        var bsonStream = new MemoryStream(syncClient.GameStateBytes);
                        using (var reader = new BsonReader(bsonStream)) {
                            var gameState = jsonSerializer.Deserialize<GameState>(reader);
                            dispatch(networkActions.SyncGameState(gameState));
                        }
                    } break;
                    case PayloadType.ReduxAction: {
                        var bsonStream = new MemoryStream(envelope.Payload);
                        using (var reader = new BsonReader(bsonStream)) {
                            var action = jsonSerializer.Deserialize(reader);
                            dispatch(action);
                        }
                    } break;
                    case PayloadType.SyncPawn:
                        var pawnSync = MessagePackSerializer.Deserialize<SyncPawn>(envelope.Payload, messagePackOptions);
                        pullPawnSync(pawnSync);
                    break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(envelope.Payload, messagePackOptions);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(envelope.Payload, messagePackOptions);
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, when);
                    } break;
                }

                dataReader.Recycle();
            };
        }

        public NetworkClient Start<T>(IPEndPoint host, IObservable<T> pollInterval) {
            _client.Start();
            _peer = _client.Connect(host, "BanchouConnectionKey");
            Debug.Log($"Connected to server at {_client.FirstPeer.EndPoint.ToString()}");

            _subscriptions.Add(
                pollInterval
                    .Subscribe(_ => {
                        _client.PollEvents();
                    })
            );

            return this;
        }

        public void Dispose() {
            Debug.Log("Client shutting down");
            _client.Stop();
            Debug.Log("Client disconnected");

            _subscriptions.Dispose();
        }
    }
}