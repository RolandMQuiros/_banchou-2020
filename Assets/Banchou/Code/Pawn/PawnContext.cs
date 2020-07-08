using System;
using UnityEngine;
using UnityEngine.AI;
using Redux;
using UniRx;

using Banchou.Player;
using Banchou.Pawn.Part;
using Banchou.Mob;

namespace Banchou.Pawn {
    public class PawnContext : MonoBehaviour, IContext, IPawnInstance {
        [SerializeField] private string _pawnId = string.Empty;

        [Header("Binding overrides")]
        [SerializeField] private Animator _animator = null;
        [SerializeField] private Rigidbody _rigidbody = null;
        [SerializeField] private Part.Orientation _orientation = null;
        [SerializeField] private NavMeshAgent _agent = null;
        private IMotor _motor = null;

        public PawnId PawnId { get; private set; } = PawnId.Create();
        public Rigidbody Body { get => _rigidbody; }
        public Transform Orientation { get => _orientation.transform; }
        public NavMeshAgent Agent { get => _agent; }
        public IMotor Motor { get => _motor; }

        public Vector3 Position { get => _rigidbody?.position ?? transform.position; }
        public Vector3 Forward { get => _orientation?.transform.forward ?? transform.forward; }

        private Dispatcher _dispatch;
        private BoardActions _boardActions;
        private MobActions _mobActions;
        private GetState _getState;
        private IObservable<GameState> _observeState;
        private IPawnInstances _pawnInstances;
        private PlayerInputStreams _playerInputStreams;

        public void Construct(
            Dispatcher dispatch,
            BoardActions boardActions,
            MobActions mobActions,
            GetState getState,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances,
            PlayerInputStreams playerInputStreams
        ) {
            _dispatch = dispatch;
            _boardActions = boardActions;
            _mobActions = mobActions;
            _getState = getState;
            _observeState = observeState;
            _pawnInstances = pawnInstances;
            _playerInputStreams = playerInputStreams;

            _animator = _animator == null ? GetComponentInChildren<Animator>(true) : _animator;
            _rigidbody =  _rigidbody == null ? GetComponentInChildren<Rigidbody>(true) : _rigidbody;
            _orientation = _orientation == null ? GetComponentInChildren<Part.Orientation>(true) : _orientation;
            _agent = _agent == null ? GetComponentInChildren<NavMeshAgent>(true) : _agent;
            _motor = _motor == null ? GetComponentInChildren<Part.IMotor>(true) : _motor;

            if (_agent != null) {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PawnId>(PawnId);
            container.Bind<Animator>(_animator);
            container.Bind<Rigidbody>(_rigidbody);
            container.Bind<Part.Orientation>(_orientation);
            container.Bind<NavMeshAgent>(_agent);

            container.Bind<Part.IMotor>(_motor);

            container.Bind<ObservePlayerLook>(
                () =>  _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInputStreams.ObserveLook(playerId))
            );
            container.Bind<ObservePlayerMove>(
                () =>  _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInputStreams.ObserveMove(playerId))
            );
        }

        private void Start() {
            // Check if the Pawn exists in the state.
            // If it doesn't, it's likely been embedded in a Scene or Prefab, which usually won't happen outside of testing.
            var state = _getState();
            if (!state.HasPawn(PawnId)) {
                // Register this context with PawnInstances
                _pawnInstances.Set(PawnId, this);

                // Let the state know about us
                _dispatch(_boardActions.Add(PawnId));

                if (_agent != null) {
                    _dispatch(_mobActions.Add(PawnId));
                }
            }
            _pawnId = PawnId.ToString();
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