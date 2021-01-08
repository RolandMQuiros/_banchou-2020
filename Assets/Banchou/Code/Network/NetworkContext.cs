using System;

using UnityEngine;
using Banchou.DependencyInjection;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;
        private GetState _getState;

        public void Construct(GetState getState) {
            _getState = getState;
        }

        private float GetServerTime() {
            if (_agent.Rollback?.Phase == RollbackPhase.Resimulate) {
                return _agent.Rollback.CorrectionTime;
            }
            return _agent.GetTime();
        }

        public void InstallBindings(DiContainer container) {
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            if (_agent != null) {
                container.Bind<NetworkActions>(new NetworkActions(_agent.GetTime));
                container.Bind<GetServerTime>(GetServerTime);
                container.Bind<IRollbackEvents>(_agent.Rollback);
            } else {
                float getLocalTime() { return Time.fixedUnscaledTime; }
                container.Bind<NetworkActions>(new NetworkActions(getLocalTime));
                container.Bind<GetServerTime>(getLocalTime);
            }
        }
    }
}