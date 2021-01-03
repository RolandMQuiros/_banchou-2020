using System;

using UnityEngine;
using Banchou.DependencyInjection;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;
        private Rollback _rollback = null;

        public void InstallBindings(DiContainer container) {
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            _rollback = _rollback ?? GetComponentInChildren<Rollback>();

            if (_agent != null && _rollback != null) {
                float GetServerTime() {
                    if (_rollback.Phase == Rollback.RollbackPhase.Resimulate) {
                        return _rollback.CorrectionTime;
                    }
                    return _agent.GetTime();
                }

                container.Bind<NetworkActions>(new NetworkActions(_agent.GetTime));
                container.Bind<IObservable<SyncPawn>>(_agent?.PulledPawnSync);
                container.Bind<PushPawnSync>(_agent.PushPawnSync);
                container.Bind<GetServerTime>(GetServerTime);
            } else {
                float getLocalTime() { return Time.fixedUnscaledTime; }
                container.Bind<NetworkActions>(new NetworkActions(getLocalTime));
                container.Bind<GetServerTime>(getLocalTime);
            }
        }
    }
}