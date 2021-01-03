using System;

using UnityEngine;

using Banchou.Board;
using Banchou.DependencyInjection;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;
        private Rollback _rollback = null;
        private GetState _getState;

        public void Construct(GetState getState) {
            _getState = getState;
        }

        private float GetServerTime() {
            if (_rollback.Phase == RollbackPhase.Resimulate) {
                return _rollback.CorrectionTime;
            }
            return _agent.GetTime();
        }

        public void InstallBindings(DiContainer container) {
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            _rollback = _rollback ?? GetComponentInChildren<Rollback>();

            if (_agent != null && _rollback != null) {
                container.Bind<NetworkActions>(new NetworkActions(_agent.GetTime));
                container.Bind<IObservable<SyncPawn>>(_agent?.PulledPawnSync);
                container.Bind<PushPawnSync>(_agent.PushPawnSync);
                container.Bind<GetServerTime>(GetServerTime);
                container.Bind<GetRollbackPhase>(() => _rollback.Phase);
            } else {
                float getLocalTime() { return Time.fixedUnscaledTime; }
                container.Bind<NetworkActions>(new NetworkActions(getLocalTime));
                container.Bind<GetServerTime>(getLocalTime);
                container.Bind<GetRollbackPhase>(() => RollbackPhase.Complete);
            }
        }
    }
}