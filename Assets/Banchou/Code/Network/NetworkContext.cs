using System;
using System.Net;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        [SerializeField] private Mode _networkMode = Mode.Local;
        private Dispatcher _dispatch;
        private NetworkActions _networkActions = new NetworkActions();

        private IDisposable _agent;
        private NetworkClient _client;
        private NetworkServer _server;

        private Subject<SyncPawn> _pulledPawnSync = new Subject<SyncPawn>();

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            PlayerInputStreams playerInput
        ) {
            _dispatch = dispatch;

            observeState
                .Select(state => state.GetNetworkMode())
                .DistinctUntilChanged()
                .Subscribe(mode => {
                    if (mode != Mode.Local && _agent != null) {
                        _agent.Dispose();
                    }

                    switch (mode) {
                        case Mode.Client:
                            _client = new NetworkClient(dispatch, playerInput, sync => _pulledPawnSync.OnNext(sync))
                                .Start(
                                    new IPEndPoint(IPAddress.Parse("localhost"), 9050),
                                    Observable.Interval(TimeSpan.FromSeconds(1))
                                );
                            _agent = _client;
                            break;
                        case Mode.Server:
                            _server = new NetworkServer(observeState, dispatch, playerActions)
                                .Start(Observable.Interval(TimeSpan.FromSeconds(1)));
                            _agent = _server;
                            break;
                    }
                })
                .AddTo(this);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IObservable<SyncPawn>>(_pulledPawnSync);
            if (_server != null) {
                container.Bind<PushPawnSync>(_server.SyncPawn);
            }
        }

        private void Start() {
            _dispatch(_networkActions.SetMode(_networkMode));
        }
    }
}