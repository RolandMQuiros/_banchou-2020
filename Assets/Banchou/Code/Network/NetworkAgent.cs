using System;
using System.Net;

using MessagePack;
using LiteNetLib;
using Newtonsoft.Json;
using Redux;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

using Banchou.Network.Message;
using Banchou.Player;

namespace Banchou.Network {
    public class NetworkAgent : MonoBehaviour, INetLogger {
        public IObservable<SyncPawn> PulledPawnSync => _pulledPawnSync;
        private IDisposable _agent;
        private NetworkClient _client;
        private NetworkServer _server;
        private Subject<SyncPawn> _pulledPawnSync = new Subject<SyncPawn>();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            PlayersActions playerActions,
            NetworkActions networkActions,
            PlayerInputStreams playerInput
        ) {
            NetDebug.Logger = this;
            var messagePackOptions = MessagePackSerializerOptions
                .Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray);
            var serializer = new JsonSerializer();

            observeState
                .Select(state => state.GetNetworkMode())
                .StartWith(Mode.Local)
                .DistinctUntilChanged()
                .Subscribe(mode => {
                    if (mode != Mode.Local && _agent != null) {
                        _agent.Dispose();
                    }

                    switch (mode) {
                        case Mode.Client:
                            _client = new NetworkClient(dispatch, networkActions, playerInput, sync => _pulledPawnSync.OnNext(sync), serializer, messagePackOptions)
                                .Start(
                                    new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9050),
                                    Observable.Interval(TimeSpan.FromSeconds(1))
                                );
                            _agent = _client;
                            break;
                        case Mode.Server:
                            _server = new NetworkServer(observeState, getState, dispatch, playerActions, playerInput, serializer, messagePackOptions)
                                .Start(this.LateUpdateAsObservable());
                            _agent = _server;
                            break;
                    }
                }).AddTo(this);
        }

        public void OnDestroy() {
            _agent?.Dispose();
        }

        public void PushPawnSync(SyncPawn syncPawn) {
            _server?.SyncPawn(syncPawn);
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args) {
            Debug.LogFormat($"{level}{str}", args);
        }
    }
}