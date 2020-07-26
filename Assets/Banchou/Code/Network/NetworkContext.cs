using System;

using UnityEngine;
using UniRx;

namespace Banchou.Network {
    public class NetworkContext : MonoBehaviour, IContext {
        public void Construct(
            IObservable<GameState> observeState
        ) {

        }

        public void InstallBindings(DiContainer container) {

        }
    }
}