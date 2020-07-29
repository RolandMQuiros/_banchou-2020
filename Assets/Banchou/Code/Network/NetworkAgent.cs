using System;
using System.Net;

using Redux;
using UnityEngine;
using UniRx;

using Banchou.Network.Message;
using Banchou.Player;

namespace Banchou.Network {
    [CreateAssetMenu(fileName = "Network Agent.asset", menuName = "Banchou/Network Agent")]
    public class NetworkAgent : ScriptableObject {
        public IObservable<SyncPawn> PulledPawnSync => _pulledPawnSync;

        private IDisposable _modeSubscription;
        private IDisposable _agent;
        private NetworkClient _client;
        private NetworkServer _server;
        private Subject<SyncPawn> _pulledPawnSync = new Subject<SyncPawn>();

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayersActions playerActions,
            PlayerInputStreams playerInput
        ) {
            _modeSubscription = _modeSubscription ?? observeState
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
                                    new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9050),
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
                });
        }

        public void PushPawnSync(SyncPawn syncPawn) {
            _server?.SyncPawn(syncPawn);
        }
    }
}