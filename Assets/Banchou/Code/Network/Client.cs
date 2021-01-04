using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using LiteNetLib;
using LiteNetLib.Utils;
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
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _client;
        private NetPeer _peer;

        private float Now => Time.fixedUnscaledTime;
        private float _lastServerTime = 0f;
        private float _lastLocalTime = 0f;

        private float _timeRequestTime = 0f;

        private CompositeDisposable _subscriptions = new CompositeDisposable();

        public NetworkClient(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayerInputStreams playerInput,
            Action<SyncPawn> pullPawnSync,
            JsonSerializer jsonSerializer,
            MessagePackSerializerOptions messagePackOptions
        ) {
            _messagePackOptions = messagePackOptions;
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            _subscriptions.Add(
                observeState
                    .Select(state => state.GetSimulatedLatency())
                    .DistinctUntilChanged()
                    .CatchIgnoreLog()
                    .Subscribe(latency => {
                        _client.SimulateLatency = latency.Min != 0 || latency.Max != 0;
                        _client.SimulationMinLatency = latency.Min;
                        _client.SimulationMaxLatency = latency.Max;
                    })
            );

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), messagePackOptions);

                // Using the type flag, check what we need to deserialize message into
                switch (envelope.PayloadType) {
                    case PayloadType.ServerTimeResponse: {
                        var response = MessagePackSerializer.Deserialize<ServerTimeResponse>(envelope.Payload, messagePackOptions);
                        // Requests are sent using unreliable delivery, so we should only care about the responses to the latest request
                        if (response.ClientTime > _lastLocalTime) {
                            // Ping compensation happens on the server, and we only need a frame of reference
                            _lastLocalTime = response.ClientTime;
                            _lastServerTime = response.ServerTime;
                        }
                    } break;
                    case PayloadType.SyncClient: {
                        var syncClient = MessagePackSerializer.Deserialize<SyncClient>(envelope.Payload, messagePackOptions);
                        var bsonStream = new MemoryStream(syncClient.GameStateBytes);
                        using (var reader = new BsonReader(bsonStream)) {
                            var gameState = jsonSerializer.Deserialize<GameState>(reader);
                            dispatch(networkActions.SyncGameState(gameState));
                            dispatch(networkActions.ConnectedToServer(syncClient.ClientNetworkId, DateTime.Now));

                            _lastLocalTime = syncClient.ClientTime;
                            _lastServerTime = syncClient.ServerTime;
                        }
                    } break;
                    case PayloadType.ReduxAction: {
                        var reduxAction = MessagePackSerializer.Deserialize<ReduxAction>(envelope.Payload, messagePackOptions);
                        var bsonStream = new MemoryStream(reduxAction.ActionBytes);
                        using (var reader = new BsonReader(bsonStream)) {
                            var action = jsonSerializer.Deserialize(reader);
                            dispatch(action);
                        }
                    } break;
                    case PayloadType.SyncPawn:
                        var pawnSync = MessagePackSerializer.Deserialize<SyncPawn>(envelope.Payload, messagePackOptions);
                        pullPawnSync(pawnSync);
                    break;
                    case PayloadType.PlayerInput: {
                        var inputUnit = MessagePackSerializer.Deserialize<InputUnit>(envelope.Payload, messagePackOptions);
                        playerInput.Push(inputUnit);
                    } break;
                }

                dataReader.Recycle();
            };

            var observeLocalPlayers = observeState
                .DistinctUntilChanged(state => state.GetPlayers())
                .Select(
                    state => new HashSet<PlayerId>(
                        state.GetPlayers()
                            .Where(p => p.Value.NetworkId == state.GetNetworkId())
                            .Select(p => p.Key)
                    )
                );

            // Transmit input
            _subscriptions.Add(
                observeLocalPlayers
                    .SelectMany(
                        localPlayers => playerInput
                            .Where(unit => unit.Type != InputUnitType.Look)
                            .Where(unit => localPlayers.Contains(unit.PlayerId))
                    )
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        var message = Envelope.CreateMessage(
                            PayloadType.PlayerInput,
                            unit,
                            _messagePackOptions
                        );
                        _peer.Send(message, DeliveryMethod.Unreliable);
                    })
            );

            Debug.Log($"Network client constructed");
        }

        /// <summary>
        /// Connects the client to the specified host server
        /// </summary>
        /// <param name="host">The <see cref="IPEndPoint"/> of the host server</param>
        /// <param name="pollInterval">How often to poll for network events. Should be comparable to framerate.</param>
        /// <param name="timeInterval">How often to poll for server time</param>
        /// <typeparam name="T">The interval unit types. Unused internally.</typeparam>
        /// <returns>This <see cref="NetworkClient"/></returns>
        public NetworkClient Start<T>(
            IPEndPoint host,
            IObservable<T> pollInterval,
            IObservable<T> timeInterval
        ) {
            _client.Start();

            var connectArgs = new NetDataWriter();
            connectArgs.Put(
                MessagePackSerializer.Serialize<ConnectClient>(
                    new ConnectClient {
                        ConnectionKey = "BanchouConnectionKey",
                        ClientConnectionTime = Now
                    },
                    _messagePackOptions
                )
            );

            _peer = _client.Connect(host, connectArgs);
            Debug.Log($"Connected to server at {_client.FirstPeer.EndPoint}");

            _subscriptions.Add(
                pollInterval
                    .CatchIgnoreLog()
                    .Subscribe(_ => { _client.PollEvents(); })
            );

            _subscriptions.Add(
                timeInterval
                    .StartWith(default(T))
                    .CatchIgnoreLog()
                    .Subscribe(_ => {
                        _timeRequestTime = Now;
                        var request = Envelope.CreateMessage(
                            PayloadType.ServerTimeRequest,
                            new ServerTimeRequest {
                                ClientTime = Now
                            },
                            _messagePackOptions
                        );
                        _peer.Send(request, DeliveryMethod.Unreliable);
                    })
            );

            return this;
        }

        /// <summary>
        /// Estimates the server time based on the last received timestamp
        /// </summary>
        /// <returns>The estimated server time</returns>
        public float GetTime() {
            return Snapping.Snap(_lastServerTime + Now - _lastLocalTime, Time.fixedUnscaledDeltaTime);
        }

        public void Dispose() {
            Debug.Log("Client shutting down");
            _client.Stop();
            Debug.Log("Client disconnected");

            _subscriptions.Dispose();
        }
    }
}