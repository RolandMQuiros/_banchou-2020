using System;
using UnityEngine;

using Banchou.DependencyInjection;

namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        private PlayerFactory _playerFactory = null;
        private PlayersActions _playerActions = new PlayersActions();
        private PlayerInputStreams _playerInputStreams = new PlayerInputStreams();

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayersActions>(_playerActions);

            _playerFactory = _playerFactory ?? GetComponentInChildren<PlayerFactory>();
            container.Bind<PlayerInputStreams>(_playerInputStreams);
        }
    }
}