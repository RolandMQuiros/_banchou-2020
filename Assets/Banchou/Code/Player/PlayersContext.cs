using System;
using UnityEngine;

using Banchou.DependencyInjection;

namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _playerParent = null;
        [SerializeField] private PlayerFactory _playerFactory = null;

        private PlayersActions _playerActions = new PlayersActions();
        private PlayerInputStreams _playerInputStreams = new PlayerInputStreams();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiator
        ) {
            _playerFactory?.Construct(_playerParent ?? transform, observeState, getState, instantiator);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayersActions>(_playerActions);
            container.Bind<PlayerInputStreams>(_playerInputStreams);
        }
    }
}