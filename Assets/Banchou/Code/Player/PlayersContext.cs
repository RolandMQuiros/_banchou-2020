using System;
using UnityEngine;

using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;

namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _playerParent = null;
        [SerializeField] private PlayerFactory _playerFactory = null;

        private PlayerActions _playerActions = new PlayerActions();
        private PlayerInputStreams _playerInputStreams = new PlayerInputStreams();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            IPawnInstances pawnInstances,
            MobActions mobActions,
            CombatantActions combatantActions,
            Instantiator instantiator
        ) {
            _playerFactory.Construct(_playerParent ?? transform, observeState, getState, instantiator);
            _playerActions.Construct(pawnInstances, mobActions, combatantActions, _playerInputStreams);
            // TODO: hook up PlayerInput detection events to state
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayerActions>(_playerActions);
            container.Bind<PlayerInputStreams>(_playerInputStreams);
        }
    }
}