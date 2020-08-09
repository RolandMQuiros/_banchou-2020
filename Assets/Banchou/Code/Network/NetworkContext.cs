using System;

using Redux;
using UnityEngine;
using Banchou.DependencyInjection;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;
        private NetworkActions _networkActions = new NetworkActions();

        public void InstallBindings(DiContainer container) {
            container.Bind<NetworkActions>(_networkActions);
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            if (_agent != null) {
                container.Bind<IObservable<SyncPawn>>(_agent?.PulledPawnSync);
                container.Bind<PushPawnSync>(_agent.PushPawnSync);
            }
        }
    }
}