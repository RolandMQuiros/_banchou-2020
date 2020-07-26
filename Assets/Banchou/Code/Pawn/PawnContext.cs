using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;
using Redux;
using UniRx;

using Banchou.Player;
using Banchou.Pawn.Part;
using Banchou.Mob;

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

        public PawnId PawnId { get; private set; } = PawnId.Create();
        public Rigidbody Body { get => _rigidbody; }
        public Transform Orientation { get => _orientation.transform; }
        public NavMeshAgent Agent { get => _agent; }
        public IMotor Motor { get => _motor; }

        public Vector3 Position { get => _rigidbody?.position ?? transform.position; }
        public Vector3 Forward { get => _orientation?.transform.forward ?? transform.forward; }

        private Dispatcher _dispatch;
        private PawnActions _pawnActions;
        private BoardActions _boardActions;
        private MobActions _mobActions;
        private GetState _getState;
        private IObservable<GameState> _observeState;
        private IPawnInstances _pawnInstances;
        private PlayerInputStreams _playerInput;

        private Subject<InputCommand> _commandSubject = new Subject<InputCommand>();
        private bool _recordStateChanges = true;
        private float _deltaTime = 0f;

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            BoardActions boardActions,
            MobActions mobActions,
            GetState getState,
            IPawnInstances pawnInstances,
            PlayerInputStreams playerInput,
            IObservable<Network.Message.SyncPawn> observePawnSyncs = null
        ) {
            _dispatch = dispatch;
            _pawnActions = new PawnActions(PawnId);
            _boardActions = boardActions;
            _mobActions = mobActions;
            _getState = getState;
            _observeState = observeState;
            _pawnInstances = pawnInstances;
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


            if (_animator != null) {
                var history = new LinkedList<PawnFSMState>();
                observeState
                    .Select(state => state.GetPawn(PawnId))
                    .Where(pawn => pawn != null)
                    .Select(pawn => pawn.FSMState)
                    .Where(s => s.StateHash != 0)
                    .DistinctUntilChanged()
                    .Subscribe(fsmState => {
                        while (history.First?.Value.FixedTimeAtChange < Time.fixedUnscaledTime - 0.5f) {
                            history.RemoveFirst();
                        }
                        history.AddLast(fsmState);
                    })
                    .AddTo(this);

                // Handle rollbacks
                observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(
                        playerId => playerInput.ObserveCommand(playerId)
                            .Select(unit => (
                                Command: unit.Command,
                                When: unit.When,
                                Diff: Time.fixedUnscaledTime - unit.When
                            ))
                    )
                    .CatchIgnore((Exception error) => { Debug.LogException(error); })
                    .Subscribe(unit => {
                        if (unit.Diff > 0f && unit.Diff < 1f) {
                            var now = Time.fixedUnscaledTime;
                            var deltaTime = Time.fixedUnscaledDeltaTime;

                            var targetState = history.Aggregate((target, step) => {
                                if (unit.When > step.FixedTimeAtChange) {
                                    return step;
                                }
                                return target;
                            });

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            _dispatch(_pawnActions.RollbackStarted());

                            // Revert to state when desync happened
                            _animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: (now - targetState.FixedTimeAtChange - deltaTime)
                                    / targetState.ClipLength
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            _dispatch(_pawnActions.FastForwarding(unit.Diff));

                            // Kick off the fast-forward. Need to run this before pushing the commands so the _animator.Play can take
                            _animator.Update(deltaTime);

                            // Pump command into a stream somewhere
                            _commandSubject.OnNext(unit.Command);

                            // Resimulate to present
                            var resimulationTime = deltaTime; // Skip the first update
                            while (resimulationTime < unit.Diff) {
                                _animator.Update(deltaTime);
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                            }
                            _dispatch(_pawnActions.RollbackComplete());
                        } else {
                            _commandSubject.OnNext(unit.Command);
                        }
                    })
                    .AddTo(this);
            }

            if (observePawnSyncs != null) {
                observePawnSyncs
                    .Subscribe(syncPawn => {
                        if (_rigidbody != null) {
                            _rigidbody.position = syncPawn.Position;
                        } else {
                            transform.position = syncPawn.Position;
                        }

                        if (_orientation != null) {
                            _orientation.transform.rotation = syncPawn.Rotation;
                        } else {
                            transform.rotation = syncPawn.Rotation;
                        }
                    })
                    .AddTo(this);
            }
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PawnId>(PawnId);
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