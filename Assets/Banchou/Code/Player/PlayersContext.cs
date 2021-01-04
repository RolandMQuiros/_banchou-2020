using System;
using UnityEngine;

using Banchou.DependencyInjection;
using Banchou.Network;

namespace Banchou.Player {
    public class PlayersContext : MonoBehaviour, IContext {
        private PlayerFactory _playerFactory = null;
        private PlayersActions _playerActions = null;
        private PlayerInputStreams _playerInputStreams = new PlayerInputStreams();

        public void Construct(GetServerTime getServerTime) {
            _playerActions = new PlayersActions(getServerTime);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<IPlayerInstances>(_playerFactory);
            container.Bind<PlayersActions>(_playerActions);

            _playerFactory = _playerFactory ?? GetComponentInChildren<PlayerFactory>();
            container.Bind<PlayerInputStreams>(_playerInputStreams);
            container.Bind<IObservable<InputUnit>>(_playerInputStreams);
        }
    }
}