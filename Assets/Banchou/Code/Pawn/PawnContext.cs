using System;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;
using UniRx;
using UniRx.Triggers;

using Banchou.DependencyInjection;
using Banchou.Network.Message;
using Banchou.Pawn.Part;
using Banchou.Player;

namespace Banchou.Pawn {
    public delegate float GetDeltaTime();

    public class PawnContext : MonoBehaviour, IContext, IPawnInstance {
        [SerializeField] private string _pawnId = string.Empty;

        [Header("Binding overrides")]
        [SerializeField] private Animator _animator = null;
        [SerializeField] private Rigidbody _rigidbody = null;
        [SerializeField] private Part.Orientation _orientation = null;
        [SerializeField] private NavMeshAgent _agent = null;
        private IMotor _motor = null;

        public PawnId PawnId { get; private set; }
        public Rigidbody Body { get => _rigidbody; }
        public Transform Orientation { get => _orientation.transform; }
        public NavMeshAgent Agent { get => _agent; }
        public IMotor Motor { get => _motor; }

        public Vector3 Position { get => _rigidbody?.position ?? transform.position; }
        public Vector3 Forward { get => _orientation?.transform.forward ?? transform.forward; }

        private PawnActions _pawnActions;
        private IObservable<GameState> _observeState;
        private PlayerInputStreams _playerInput;

        private Subject<InputCommand> _commandSubject = new Subject<InputCommand>();
        private float _deltaTime = 0f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> onStateUpdate,
            PlayerInputStreams playerInput,
            IObservable<SyncPawn> onPawnSync = null
        ) {
            PawnId = pawnId;
            _observeState = onStateUpdate;
            _pawnActions = new PawnActions(PawnId);
            _playerInput = playerInput;

            _animator = _animator == null ? GetComponentInChildren<Animator>(true) : _animator;
            _rigidbody =  _rigidbody == null ? GetComponentInChildren<Rigidbody>(true) : _rigidbody;
            _orientation = _orientation == null ? GetComponentInChildren<Part.Orientation>(true) : _orientation;
            _agent = _agent == null ? GetComponentInChildren<NavMeshAgent>(true) : _agent;
            _motor = _motor == null ? GetComponentInChildren<Part.IMotor>(true) : _motor;

            if (_agent != null) {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }

            if (onPawnSync != null) {
                this.FixedUpdateAsObservable()
                    .WithLatestFrom(
                        onPawnSync.Where(syncPawn => syncPawn.PawnId == PawnId),
                        (_, syncPawn) => syncPawn
                    )
                    .Subscribe(syncPawn => {
                        var newPosition = Vector3.MoveTowards(
                            transform.position,
                            syncPawn.Position,
                            Mathf.Pow(10f, Mathf.Floor(Mathf.Log10((transform.position - syncPawn.Position).magnitude)))
                        );

                        var delta = newPosition - transform.position;
                        if (delta.magnitude > 1f) {
                            Debug.Log($"Jumped from {transform.position} to {newPosition}");
                        }

                        transform.position = newPosition;

                        var targetRotation = Quaternion.LookRotation(syncPawn.Forward);
                        if (_orientation != null) {
                            _orientation.transform.rotation = Quaternion.RotateTowards(
                                _orientation.transform.rotation,
                                targetRotation,
                                Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Quaternion.Angle(_orientation.transform.rotation, targetRotation))))
                            );
                        } else {
                            transform.rotation = Quaternion.RotateTowards(
                                transform.rotation,
                                Quaternion.LookRotation(syncPawn.Forward),
                                Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Quaternion.Angle(transform.rotation, targetRotation))))
                            );
                        }

                        if (_animator != null && syncPawn.StateHash != 0) {
                            _animator.Play(syncPawn.StateHash, 0, syncPawn.StateNormalizedTime);
                        }
                    })
                    .AddTo(this);
            }

            _pawnId = PawnId.ToString();
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PawnId>(PawnId);
            container.Bind<IPawnInstance>(this);
            container.Bind<Animator>(_animator);
            container.Bind<Rigidbody>(_rigidbody);
            container.Bind<Part.Orientation>(_orientation);
            container.Bind<NavMeshAgent>(_agent);
            container.Bind<Part.IMotor>(_motor);
            container.Bind<PawnActions>(_pawnActions);
            container.Bind<GetDeltaTime>(() => _deltaTime);

            container.Bind<ObservePlayerLook>(
                () => _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInput.ObserveLook(playerId).Select(unit => unit.Look))
            );

            container.Bind<ObservePlayerMove>(
                () => _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInput.ObserveMove(playerId).Select(unit => unit.Move))
            );

            container.Bind<ObservePlayerCommand>(() => _commandSubject);
            container.Bind<Subject<InputCommand>>(_commandSubject, t => t == typeof(Part.Rollback));
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