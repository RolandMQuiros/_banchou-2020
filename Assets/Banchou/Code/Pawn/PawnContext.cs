using System;
using System.Linq;
using Redux;

using UnityEngine;
using UnityEngine.AI;
using UniRx;

using Banchou.DependencyInjection;
using Banchou.Network;
using Banchou.Pawn.Part;
using Banchou.Player;

namespace Banchou.Pawn {
    public delegate float GetDeltaTime();

    public class PawnContext : MonoBehaviour, IContext, IPawnInstance {
        [SerializeField] private string _pawnId = string.Empty;

        [Header("Binding overrides")]
        [SerializeField] private Animator _animator = null;
        [SerializeField] private Rigidbody _rigidbody = null;
        [SerializeField] private CharacterController _controller = null;
        [SerializeField] private Part.Orientation _orientation = null;
        [SerializeField] private NavMeshAgent _agent = null;
        [SerializeField] private Part.PawnRollback _pawnRollback = null;
        private IMotor _motor = null;

        #region IPawnInstance
        public PawnId PawnId { get; private set; }
        public Vector3 Position {
            get => transform.position;
            set => transform.position = value;
        }
        public Vector3 Forward {
            get => _orientation?.transform.forward ?? transform.forward;
            set {
                if (_orientation != null) {
                    _orientation.transform.rotation = Quaternion.LookRotation(value);
                } else {
                    transform.rotation = Quaternion.LookRotation(value);
                }
            }
        }
        #endregion

        private Dispatcher _dispatch;
        private GetState _getState;
        private PawnActions _pawnActions;
        private IObservable<GameState> _observeState;
        private PlayerInputStreams _playerInput;

        public void Construct(
            PawnId pawnId,
            Dispatcher dispatch,
            GetState getState,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInput,
            GetServerTime getServerTime
        ) {
            PawnId = pawnId;
            _dispatch = dispatch;
            _getState = getState;
            _observeState = observeState;
            _pawnActions = new PawnActions(PawnId, getServerTime);
            _playerInput = playerInput;

            _animator = _animator == null ? GetComponentInChildren<Animator>(true) : _animator;
            _rigidbody =  _rigidbody == null ? GetComponentInChildren<Rigidbody>(true) : _rigidbody;
            _controller = _controller == null ? GetComponentInChildren<CharacterController>(true) : _controller;
            _orientation = _orientation == null ? GetComponentInChildren<Part.Orientation>(true) : _orientation;
            _agent = _agent == null ? GetComponentInChildren<NavMeshAgent>(true) : _agent;
            _motor = _motor == null ? GetComponentInChildren<Part.IMotor>(true) : _motor;
            _pawnRollback = _pawnRollback == null ? GetComponentInChildren<Part.PawnRollback>(true) : _pawnRollback;

            if (_agent != null) {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }

            _pawnId = PawnId.ToString();
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PawnId>(PawnId);
            container.Bind<IPawnInstance>(this);
            container.Bind<Animator>(_animator);
            container.Bind<Rigidbody>(_rigidbody);
            container.Bind<CharacterController>(_controller);
            container.Bind<Part.Orientation>(_orientation);
            container.Bind<NavMeshAgent>(_agent);
            container.Bind<Part.IMotor>(_motor);
            container.Bind<Part.PawnRollback>(_pawnRollback);
            container.Bind<PawnActions>(_pawnActions);

            // Short-circuit dispatcher for client facade pawns
            container.Bind<Redux.Dispatcher>(action => {
                if (_getState().IsServer()) {
                    return _dispatch(action);
                }
                return action;
            });

            container.Bind<ObservePlayerLook>(
                () => _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(
                        playerId => _playerInput
                            .ObserveLook(playerId)
                            .Select(unit => unit.Look)
                    )
            );

            container.Bind<ObservePlayerMove>(() => _observeState
                .Select(state => state.GetPawnPlayerId(PawnId))
                .DistinctUntilChanged()
                .SelectMany(playerId => _playerInput.ObserveMoves(playerId))
                .Select(unit => unit.Direction)
                .DistinctUntilChanged()
            );
            container.Bind<ObservePlayerCommand>(() => _observeState
                .Select(state => state.GetPawnPlayerId(PawnId))
                .SelectMany(playerId => _playerInput.ObserveCommands(playerId))
                .Select(unit => unit.Command)
            );
        }

        private void OnDrawGizmos() {
            if (_agent != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_agent.nextPosition, 0.25f);

                if (_agent.path?.corners != null) {
                    Gizmos.color = Color.blue;
                    for (int i = 0; i < _agent.path.corners.Length; i++) {
                        Gizmos.DrawWireSphere(_agent.path.corners[i], 0.25f);
                    }
                }
            }
        }
    }
}