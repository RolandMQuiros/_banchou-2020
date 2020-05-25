using UnityEngine;
using Redux;

using Banchou.Pawn;

using Banchou.Player.Activation;
using Banchou.Player.Targeting;

namespace Banchou.Player {
    public class PlayerContext : MonoBehaviour, IContext {
        public PlayerId PlayerId { get; set; } = PlayerId.Create();

        [SerializeField] private InputSource _playerInputSource = InputSource.AI;

        private GetState _getState;
        private Dispatcher _dispatch;
        private PlayerActions _playerActions;
        private TargetingActions _targetingActions;
        private ActivationActions _activationActions;
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

            _targetingActions = new TargetingActions(pawnInstances);
            _activationActions = new ActivationActions();
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PlayerId>(PlayerId);
            container.Bind<Instantiator>(Instantiate);
            container.Bind<TargetingActions>(_targetingActions);
            container.Bind<ActivationActions>(_activationActions);
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