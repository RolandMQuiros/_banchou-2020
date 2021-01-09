using System;

using UnityEngine;
using Banchou.DependencyInjection;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;
        private NetworkActions _networkActions;

        public void Construct(GetTime getLocalTime) {
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            if (_agent == null) {
                _networkActions = new NetworkActions(getLocalTime);
            } else {
                _networkActions = new NetworkActions((GetTime)_agent.GetTime);
            }
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<NetworkActions>(_networkActions);
            if (_agent != null) {
                container.Bind<NetworkActions>(_networkActions);
                container.Bind<GetTime>((GetTime)_agent.GetTime);
                container.Bind<GetDeltaTime>((GetDeltaTime)_agent.GetDeltaTime);
                container.Bind<IRollbackEvents>(_agent.Rollback);
            }
        }
    }
}