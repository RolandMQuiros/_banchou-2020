using System;

using UnityEngine;
using Banchou.DependencyInjection;
using Banchou.Network.Message;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        private NetworkAgent _agent = null;

        public void InstallBindings(DiContainer container) {
            _agent = _agent ?? GetComponentInChildren<NetworkAgent>();
            if (_agent != null) {
                container.Bind<NetworkActions>(new NetworkActions(_agent.GetTime));
                container.Bind<IObservable<SyncPawn>>(_agent?.PulledPawnSync);
                container.Bind<PushPawnSync>(_agent.PushPawnSync);
                container.Bind<GetServerTime>(_agent.GetTime);
            } else {
                float getLocalTime() { return Time.fixedUnscaledTime; }
                container.Bind<NetworkActions>(new NetworkActions(getLocalTime));
                container.Bind<GetServerTime>(getLocalTime);
            }
        }
    }
}