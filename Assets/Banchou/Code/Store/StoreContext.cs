using System;
using UnityEngine;
using Redux;

using Banchou.DependencyInjection;

namespace Banchou {
    public class StoreContext : MonoBehaviour, IContext {
        [SerializeField] private GameStateStore _store = null;
        public GameStateStore Store => _store;
        public void Construct() {
            if (!_store.IsInitialized) {
                _store.Initialize();
            }
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IObservable<GameState>>(_store.ObserveState());
            container.Bind<Dispatcher>(_store.Dispatch);
            container.Bind<GetState>(_store.GetState);
            container.Bind<GetTime>(_ => Time.fixedTime);
            container.Bind<GetDeltaTime>(_ => Time.fixedDeltaTime);
        }
    }
    public delegate GameState GetState();
}