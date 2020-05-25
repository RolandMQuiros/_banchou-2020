using System;
using UnityEngine;

using Banchou.Pawn;
namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _playerParent = null;
        [SerializeField] private PlayerFactory _playerFactory = null;

        private PlayerActions _playerActions = new PlayerActions();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiator
        ) {
            _playerFactory.Construct(_playerParent ?? transform, observeState, getState, instantiator);
            // TODO: hook up PlayerInput detection events to state
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayerActions>(_playerActions);
        }
    }
}