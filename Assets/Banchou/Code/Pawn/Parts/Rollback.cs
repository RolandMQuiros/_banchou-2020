using System;
using System.Collections.Generic;
using System.Linq;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class Rollback : MonoBehaviour {
        [SerializeField] private float _historyWindow = 3f;
        [SerializeField] private float _rollbackThreshold = 0.05f;
        [SerializeField] private float _stateTimeThreshold = 0.005f;

        [SerializeField] private float _commandDebounce = 0.01f;

        private PawnId _pawnId;
        private GetServerTime _getServerTime;
        private LinkedList<PawnFSMState> _history = new LinkedList<PawnFSMState>();
        public PawnRollbackState State { get; private set; }

        public float FastForwardStartTime { get; private set; }
        public float FastForwardCurrentTime { get; private set; }

        [SerializeField] private PawnFSMState[] _debugHistory = null;

        [Serializable]
        private struct InputUnit {
            public InputCommand Command;
            public Vector3 Move;
            public float When;
            public float Diff;
        }
        [SerializeField] private InputUnit[] _debugBuffer = null;

        public Rollback PushStateChange(int stateHash, bool isLoop, float clipLength, float when) {
            while (_history.Count > 1 && _history.First.Value.FixedTimeAtChange < _getServerTime() - _historyWindow) {
                _history.RemoveFirst();
            }

            // Only distinct changes
            if (_history.Count == 0 || _history.Last.Value.StateHash != stateHash) {
                _history.AddLast(
                    new PawnFSMState {
                        PawnId = _pawnId,
                        StateHash = stateHash,
                        IsLoop = isLoop,
                        ClipLength = clipLength,
                        FixedTimeAtChange = when
                    }
                );
            }

            _debugHistory = _history.ToArray();

            return this;
        }

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PawnActions pawnActions,
            PlayerInputStreams playerInput,
            Subject<InputCommand> commandSubject,
            Subject<Vector3> moveSubject,
            GetServerTime getServerTime,
            Animator animator = null
        ) {
            _getServerTime = getServerTime;

            if (animator != null) {
                var movesAndCommands = observeState
                    .Select(state => state.GetPawnPlayerId(pawnId))
                    .DistinctUntilChanged()
                    .SelectMany(
                        playerId => playerInput.ObserveCommand(playerId)
                            .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                            .Select(unit => new InputUnit {
                                Command = unit.Command,
                                When = unit.When,
                                Diff = getServerTime() - unit.When
                            })
                            .Throttle(TimeSpan.FromSeconds(_commandDebounce))
                            .Merge(playerInput.ObserveMove(playerId)
                                .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                                .Select(unit => new InputUnit {
                                    Move = unit.Move,
                                    When = unit.When,
                                    Diff = getServerTime() - unit.When
                                })
                            )
                    );

                // Aggregate FSM changes into a list
                var history = new LinkedList<PawnFSMState>();
                observeState
                    .Select(state => state.GetLatestFSMChange())
                    .DistinctUntilChanged()
                    .Where(change => change.PawnId == pawnId && change.StateHash != 0)
                    .DistinctUntilChanged(s => s.StateHash)
                    .Subscribe(fsmState => {
                        // Always have at least one state change on the list
                        while (history.Count > 1 && history.First.Value.FixedTimeAtChange < getServerTime() - _historyWindow) {
                            history.RemoveFirst();
                        }
                        history.AddLast(fsmState);
                    })
                    .AddTo(this);

                var lastTargetNormalizedTime = 0f;
                // Handle rollbacks
                movesAndCommands
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        // currently don't handle movement rollback, since we don't keep a transform history yet
                        if (unit.Diff > _rollbackThreshold && State == PawnRollbackState.Complete && unit.Command != InputCommand.None) {
                            var deltaTime = Mathf.Min(unit.Diff, Time.fixedUnscaledDeltaTime);

                            var targetState = history.Aggregate((target, step) => {
                                if (unit.When > step.FixedTimeAtChange) {
                                    return step;
                                }
                                return target;
                            });

                            // Revert to state when desync happened
                            var timeSinceStateStart = getServerTime() - targetState.FixedTimeAtChange;
                            var targetNormalizedTime = timeSinceStateStart % targetState.ClipLength;

                            // If a previous command has rewound to a similar time, within some threshold, don't bother rolling back??????
                            if (Mathf.Abs(targetNormalizedTime - lastTargetNormalizedTime) < _stateTimeThreshold) {
                                return;
                            }

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            dispatch(pawnActions.RollbackStarted()); // For the server
                            State = PawnRollbackState.RollingBack; // For the client

                            animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: targetNormalizedTime
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            dispatch(pawnActions.FastForwarding(unit.Diff)); // Server
                            State = PawnRollbackState.FastForward; // Client
                            FastForwardStartTime = getServerTime() - unit.Diff;

                            // Kick off the fast-forward. Need to run this before pushing the commands so the _animator.Play can take
                            animator.Update(deltaTime);

                            // Pump input into streams
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                                Debug.Log($"Move {unit.Move} Rollback:\n" +
                                    $"Server time at packet send: {unit.When}\n" +
                                    $"\tServer time since packet sent: {getServerTime() - unit.When}\n" +
                                    $"Server time at previous state start: {targetState.FixedTimeAtChange}\n" +
                                    $"\tServer time since previous state start: {timeSinceStateStart}\n" +
                                    $"Target Normalized Time: {targetNormalizedTime}\n" +
                                    $"Server Time: {getServerTime()}"
                                );
                            } else {
                                commandSubject.OnNext(unit.Command);
                                Debug.Log($"Command {unit.Command} Rollback:\n" +
                                    $"Server time at packet send: {unit.When}\n" +
                                    $"\tServer time since packet sent: {getServerTime() - unit.When}\n" +
                                    $"Server time at previous state start: {targetState.FixedTimeAtChange}\n" +
                                    $"\tServer time since previous state start: {timeSinceStateStart}\n" +
                                    $"Target Normalized Time: {targetNormalizedTime}\n" +
                                    $"Server Time: {getServerTime()}"
                                );
                            }

                            // Resimulate to present
                            var resimulationTime = deltaTime; // Skip the first update
                            while (resimulationTime < unit.Diff) {
                                animator.Update(Mathf.Min(deltaTime, unit.Diff - resimulationTime));
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                            }

                            dispatch(pawnActions.RollbackComplete());
                            State = PawnRollbackState.Complete;
                        } else {
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);

                                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                                Debug.Log($"Command {unit.Command}:\n" +
                                    $"Server Time: {getServerTime()}\n" +
                                    $"When: {unit.When}\n" +
                                    $"Normalized Time: {stateInfo.normalizedTime}"
                                );
                            }
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}