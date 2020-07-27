using System;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.DependencyInjection;
using Banchou.Player;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        [SerializeField] private NetworkAgent _agent = null;
        private Dispatcher _dispatch;
        private NetworkActions _networkActions = new NetworkActions();

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            PlayerInputStreams playerInput
        ) {
            _agent.Construct(observeState, dispatch, playerActions, playerInput);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IObservable<SyncPawn>>(_agent.PulledPawnSync);
            container.Bind<NetworkActions>(_networkActions);
            container.Bind<PushPawnSync>(_agent.PushPawnSync);
        }
    }
}