﻿using System;
using UnityEngine;
using Redux;

namespace Banchou {
    public class StoreContext : MonoBehaviour, IContext {
        [SerializeField] private GameStateStore _store = null;

        public void InstallBindings(DiContainer container) {
            container.Bind<IObservable<GameState>>(_store.ObserveState());
            container.Bind<Dispatcher>(_store.Dispatch);
            container.Bind<GetState>(_store.GetState);
        }
    }
    public delegate GameState GetState();
}