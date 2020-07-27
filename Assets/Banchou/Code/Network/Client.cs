using System;
using System.Net;

using LiteNetLib;
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

            // Receiving data from server
            _listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                // Calculate when the event was sent
                var when = Time.fixedUnscaledTime - (fromPeer.Ping / 1000f);

                Envelope envelope = new Envelope();
                dataReader.RawData.ToObject(ref envelope);

                switch (envelope.PayloadType) {
                    case PayloadType.Action:
                        dispatch(envelope.Payload);
                    break;
                    case PayloadType.SyncPawn:
                        pullPawnSync((SyncPawn)envelope.Payload);
                    break;
                    case PayloadType.PlayerCommand: {
                        var playerCommand = (PlayerCommand)envelope.Payload;
                        playerInput.PushCommand(playerCommand.PlayerId, playerCommand.Command, when);
                    } break;
                    case PayloadType.PlayerMove: {
                        var playerMove = (PlayerMove)envelope.Payload;
                        playerInput.PushMove(playerMove.PlayerId, playerMove.Direction, when);
                    } break;
                }

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