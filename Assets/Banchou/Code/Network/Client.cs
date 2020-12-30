﻿using System;
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

using Stopwatch = System.Diagnostics.Stopwatch;

#pragma warning disable 0618

namespace Banchou.Network {
    public class NetworkClient : IDisposable {
        private MessagePackSerializerOptions _messagePackOptions;
        private EventBasedNetListener _listener;
        private NetManager _client;
        private NetPeer _peer;

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        private long _lastServerTime = 0;
        private long _lastLocalTime = 0;

        private CompositeDisposable _subscriptions = new CompositeDisposable();

        public NetworkClient(
            IObservable<GameState> onStateUpdate,
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

            // _client.SimulateLatency = false;
            // _client.SimulationMinLatency = 600;
            // _client.SimulationMaxLatency = 800;

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = _stopwatch.ElapsedTicks - TimeSpan.FromMilliseconds(fromPeer.Ping / 2).Ticks;
                var envelope = MessagePackSerializer.Deserialize<Envelope>(dataReader.GetRemainingBytes(), messagePackOptions);

                // Using the type flag, check what we need to deserialize message into
                switch (envelope.PayloadType) {
                    case PayloadType.ServerTimeResponse: {
                        var response = MessagePackSerializer.Deserialize<ServerTimeResponse>(envelope.Payload, messagePackOptions);
                        // Requests are sent using unreliable delivery, so we should only care about the responses to the latest request
                        if (response.ClientTime == _lastLocalTime) {
                            // Ping compensation happens on the server, and we only need a frame of reference
                            _lastServerTime = response.ServerTime;
                        }
                    } break;
                    case PayloadType.SyncClient: {
                        var syncClient = MessagePackSerializer.Deserialize<SyncClient>(envelope.Payload, messagePackOptions);
                        var bsonStream = new MemoryStream(syncClient.GameStateBytes);
                        using (var reader = new BsonReader(bsonStream)) {
                            var gameState = jsonSerializer.Deserialize<GameState>(reader);
                            dispatch(networkActions.SyncGameState(gameState));
                            dispatch(networkActions.ConnectedToServer(syncClient.ClientNetworkId, new DateTime(syncClient.ServerTime)));

                            Debug.Log($"Time at connect: {_lastLocalTime}\nTime at response: {_stopwatch.ElapsedTicks}\nDiff:{_stopwatch.ElapsedTicks - _lastLocalTime}\nReceived Client Time:{syncClient.ClientTime}\nReceived Server Time:{syncClient.ServerTime}");
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
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(envelope.Payload, messagePackOptions);
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, playerMove.When);
                    } break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(envelope.Payload, messagePackOptions);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, playerCommand.When);
                    } break;
                }

                dataReader.Recycle();
            };

            var observeLocalPlayers = onStateUpdate
                .DistinctUntilChanged(state => state.GetPlayers())
                .Select(
                    state => new HashSet<PlayerId>(
                        state.GetPlayers()
                            .Where(p => p.Value.NetworkId == state.GetNetworkId())
                            .Select(p => p.Key)
                    )
                );

            _subscriptions.Add(
                observeLocalPlayers
                    .SelectMany(
                        localPlayers => playerInput
                            .ObserveCommand()
                            .Where(unit => localPlayers.Contains(unit.PlayerId))
                    )
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        var message = Envelope.CreateMessage(
                            PayloadType.PlayerCommand,
                            new PlayerCommand {
                                PlayerId = unit.PlayerId,
                                Command = unit.Command,
                                When = unit.When
                            },
                            _messagePackOptions
                        );
                        _peer.Send(message, DeliveryMethod.Unreliable);
                    })
            );

            _subscriptions.Add(
                observeLocalPlayers
                    .SelectMany(
                        localPlayers => playerInput
                            .ObserveMove()
                            .Where(unit => localPlayers.Contains(unit.PlayerId))
                    )
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        var message = Envelope.CreateMessage(
                            PayloadType.PlayerMove,
                            new PlayerMove {
                                PlayerId = unit.PlayerId,
                                Direction = unit.Move,
                                When = unit.When
                            },
                            _messagePackOptions
                        );
                        _peer.Send(message, DeliveryMethod.Unreliable);
                    })
            );

            _lastLocalTime = _stopwatch.ElapsedTicks;
            Debug.Log($"Network client constructed {Stopwatch.Frequency}");
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
                        ClientConnectionTime = _stopwatch.ElapsedTicks
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
                        _lastLocalTime = _stopwatch.ElapsedTicks;
                        var request = Envelope.CreateMessage(
                            PayloadType.ServerTimeRequest,
                            new ServerTimeRequest {
                                ClientTime = _lastLocalTime
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
            return (float)TimeSpan.FromTicks(_lastServerTime + (_stopwatch.ElapsedTicks - _lastLocalTime)).TotalSeconds;
        }

        public void Dispose() {
            Debug.Log("Client shutting down");
            _client.Stop();
            Debug.Log("Client disconnected");

            _subscriptions.Dispose();
        }
    }
}