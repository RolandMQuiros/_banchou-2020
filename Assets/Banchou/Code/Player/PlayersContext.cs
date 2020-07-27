using System;
using UnityEngine;

using Banchou.Combatant;
using Banchou.DependencyInjection;
using Banchou.Mob;
using Banchou.Pawn;

namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _playerParent = null;
        [SerializeField] private PlayerFactory _playerFactory = null;

        private PlayerActions _playerActions;
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
            _playerActions = new PlayerActions(pawnInstances, combatantActions);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayerActions>(_playerActions);
            container.Bind<PlayerInputStreams>(_playerInputStreams);
        }
    }
}