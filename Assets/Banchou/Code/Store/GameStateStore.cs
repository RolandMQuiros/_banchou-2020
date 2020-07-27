using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using Newtonsoft.Json;
using Redux;
using Redux.Reactive;

using Banchou.Board;
using Banchou.Combatant;
using Banchou.Mob;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou {
    namespace StateAction {
        public class SceneLoaded { }
    }

    [CreateAssetMenu(fileName = "GameStateStore.asset", menuName = "Banchou/Game State Store")]
    public class GameStateStore : ScriptableObject, IStore<GameState> {
        [SerializeField] private TextAsset _initialState = null;
        [SerializeField] private Redux.DevTools.DevToolsSession _devToolsSession = null;
        private IStore<GameState> _store;

        public event Action StateChanged {
            add { _store.StateChanged += value; }
            remove { _store.StateChanged -= value; }
        }

        public object Dispatch(object action) {
            return _store.Dispatch(action);
        }

        public GameState GetState() {
            return _store.GetState();
        }

        public IObservable<GameState> ObserveState() {
            return _store.ObserveState();
        }

        private void OnEnable() {
            var initialState = JsonConvert.DeserializeObject<GameState>(_initialState.text);

            if (_devToolsSession == null) {
                _store = new Store<GameState>(
                    Reducer, initialState, Redux.Middlewares.Thunk, NetworkServer.Install<GameState>()
                );
            } else {
                _store = new Store<GameState>(
                    Reducer, initialState, Redux.Middlewares.Thunk, NetworkServer.Install<GameState>(), _devToolsSession.Install<GameState>()
                );
            }

            SceneManager.sceneLoaded += (scene, loadSceneMode) => {
                if (loadSceneMode == LoadSceneMode.Single) {
                    _store.Dispatch(new StateAction.SceneLoaded());
                }
            };
        }

        protected virtual GameState Reducer(in GameState prev, in object action) {
            return new GameState {
                Network = NetworkReducers.Reduce(prev.Network, action),
                Board = BoardReducers.Reduce(prev.Board, action),
                Players = PlayerReducers.Reduce(prev.Players, action),
                Pawns = PawnsReducers.Reduce(prev.Pawns, action),
                PawnSync = PawnSyncReducers.Reduce(prev.PawnSync, action),
                Mobs = MobsReducers.Reduce(prev.Mobs, action),
                Combatants = CombatantsReducers.Reduce(prev.Combatants, action)
            };
        }
    }
}