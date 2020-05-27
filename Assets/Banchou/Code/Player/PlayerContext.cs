using UnityEngine;
using Redux;
using Banchou.Pawn;

namespace Banchou.Player {
    public class PlayerContext : MonoBehaviour, IContext {
        public PlayerId PlayerId { get; set; } = PlayerId.Create();

        [SerializeField] private InputSource _playerInputSource = InputSource.AI;

        private GetState _getState;
        private Dispatcher _dispatch;
        private PlayerActions _playerActions;
        private IPlayerInstances _playerInstances;

        public void Construct(
            GetState getState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            IPlayerInstances playerInstances,
            IPawnInstances pawnInstances
        ) {
            _getState = getState;
            _dispatch = dispatch;
            _playerActions = playerActions;
            _playerInstances = playerInstances;
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PlayerId>(PlayerId);
        }

        private void Start() {
            var state = _getState();
            var player = state.GetPlayer(PlayerId);
            if (player == null) {
                _playerInstances.Set(PlayerId, gameObject);
                _dispatch(_playerActions.Add(PlayerId, _playerInputSource));
            }
        }
    }

}