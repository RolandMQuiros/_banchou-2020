using System;
using UnityEngine;

using MessagePack;
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
        public struct Hydrate {
            public GameState GameState;
        }
    }

    [CreateAssetMenu(fileName = "GameStateStore.asset", menuName = "Banchou/Game State Store")]
    public class GameStateStore : ScriptableObject, IStore<GameState> {
        [SerializeField] private TextAsset _initialState = null;
        [SerializeField] private Redux.DevTools.DevToolsSession _devToolsSession = null;
        private IStore<GameState> _store;

        public bool IsInitialized => _store != null;

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

        public void Initialize() {
            if (_store != null) { return; }

            var initialState = JsonConvert.DeserializeObject<GameState>(_initialState.text);
            var jsonSerializer = new JsonSerializer();
            var messagePackOptions = MessagePackSerializerOptions
                .Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray);;

            if (_devToolsSession == null) {
                _store = new Store<GameState>(
                    Reducer, initialState, Redux.Middlewares.Thunk, NetworkServer.Install<GameState>(jsonSerializer, messagePackOptions)
                );
            } else {
                _store = new Store<GameState>(
                    Reducer, initialState, Redux.Middlewares.Thunk, NetworkServer.Install<GameState>(jsonSerializer, messagePackOptions), _devToolsSession.Install<GameState>()
                );
            }
        }

        public static GameState Reducer(in GameState prev, in object action) {
            if (action is StateAction.Hydrate hydrate) {
                return hydrate.GameState;
            }

            return new GameState {
                Network = NetworkReducers.Reduce(prev.Network, action),
                Board = BoardReducers.Reduce(prev.Board, action),
                Players = PlayerReducers.Reduce(prev.Players, prev.Network, action),
                Pawns = PawnsReducers.Reduce(prev.Pawns, action),
                PawnSync = PawnSyncReducers.Reduce(prev.PawnSync, action),
                Mobs = MobsReducers.Reduce(prev.Mobs, action),
                Combatants = CombatantsReducers.Reduce(prev.Combatants, action)
            };
        }
    }
}