using System;
using System.Net;

using LiteNetLib;
using MessagePack;
using MessagePack.Resolvers;
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
            NetworkActions networkActions,
            PlayerInputStreams playerInput,
            Action<SyncPawn> pullPawnSync
        ) {
            _listener = new EventBasedNetListener();
            _client = new NetManager(_listener);

            var buffer = new byte[256];

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);

                // Extract payload type
                var payloadType = (PayloadType)dataReader.GetByte();

                // Expand the buffer if needed
                if (dataReader.AvailableBytes > buffer.Length) {
                    Array.Resize(ref buffer, dataReader.AvailableBytes);
                }

                // Read the remainder of the message into the buffer
                dataReader.GetBytes(buffer, dataReader.AvailableBytes);

                // Using the type flag, check what we need to deserialize message into
                switch (payloadType) {
                    case PayloadType.SyncClient:
                        var syncClient = MessagePackSerializer.Deserialize<SyncClient>(buffer);
                        dispatch(networkActions.SyncGameState(syncClient.GameState));
                    break;
                    case PayloadType.ReduxAction:
                        var action = MessagePackSerializer.Deserialize<object>(buffer, ContractlessStandardResolver.Options);
                        dispatch(action);
                    break;
                    case PayloadType.SyncPawn:
                        var pawnSync = MessagePackSerializer.Deserialize<SyncPawn>(buffer);
                        pullPawnSync(pawnSync);
                    break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = MessagePackSerializer.Deserialize<PlayerCommand>(buffer);
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = MessagePackSerializer.Deserialize<PlayerMove>(buffer);
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