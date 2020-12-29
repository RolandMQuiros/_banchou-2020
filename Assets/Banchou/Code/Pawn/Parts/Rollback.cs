using System;
using System.Collections.Generic;
using System.Linq;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;
using Banchou.Network.Message;

namespace Banchou.Pawn.Part {
    public class Rollback : MonoBehaviour {
        [SerializeField, Tooltip("How much state history to record, in seconds")]
        private float _historyWindow = 3f;
        [SerializeField, Tooltip("Minimum delay between the current time and an input's timestamp before kicking off a rollback")]
        private float _rollbackThreshold = 0.05f;
        [SerializeField, Tooltip("How much time after a successful rollback before accepting another rollback")]
        private float _rollbackDebounce = 0.032f;
        public enum RollbackState : byte {
            Complete,
            RollingBack,
            FastForward
        }
        public RollbackState State { get; private set; }
        public float CorrectionTime { get; private set; }

        private GetServerTime _getServerTime;

        [Serializable]
        private struct InputUnit {
            public InputCommand Command;
            public Vector3 Move;
            public float When;
            public float Diff;
        }

        public void Construct(
            PawnId pawnId,
            IPawnInstance pawn,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PawnActions pawnActions,
            PlayerInputStreams playerInput,
            Subject<InputCommand> commandSubject,
            Subject<Vector3> moveSubject,
            IObservable<SyncPawn> onSyncPawn,
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
                var fsmHistory = new LinkedList<PawnFSMState>();

                observeState
                    .Select(state => state.GetLatestFSMChange())
                    .DistinctUntilChanged()
                    .Where(change => change.PawnId == pawnId && change.StateHash != 0)
                    .DistinctUntilChanged(s => s.StateHash)
                    .Subscribe(fsmState => {
                        // Always have at least one state change on the list
                        while (fsmHistory.Count > 1 && fsmHistory.First.Value.When < getServerTime() - _historyWindow) {
                            fsmHistory.RemoveFirst();
                        }
                        fsmHistory.AddLast(fsmState);
                    })
                    .AddTo(this);

                var lastTargetNormalizedTime = 0f;
                // Handle rollbacks
                movesAndCommands
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        if (unit.Diff > _rollbackThreshold && State == RollbackState.Complete) {
                            var deltaTime = Mathf.Min(unit.Diff, Time.fixedUnscaledDeltaTime);

                            var targetState = fsmHistory.Aggregate((target, step) => {
                                if (unit.When > step.When) {
                                    return step;
                                }
                                return target;
                            });

                            // Revert to state when desync happened
                            var timeSinceStateStart = getServerTime() - targetState.When;
                            var targetNormalizedTime = timeSinceStateStart % targetState.ClipLength;

                            // If a previous command has rewound to a similar time, within some threshold, don't bother rolling back
                            var debouncedTime = Mathf.Abs(targetNormalizedTime - lastTargetNormalizedTime);
                            if (debouncedTime < _rollbackDebounce) {
                                return;
                            }
                            lastTargetNormalizedTime = targetNormalizedTime;

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            State = RollbackState.RollingBack; // For the client

                            // Reposition/rotate to where the pawn was at time of rollback
                            var t = unit.Diff / (getServerTime() - targetState.When);
                            pawn.Position = Vector3.Lerp(targetState.Position, pawn.Position, t);
                            pawn.Forward = Vector3.Lerp(targetState.Forward, pawn.Forward, t);

                            animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: targetNormalizedTime
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            State = RollbackState.FastForward; // Client
                            CorrectionTime = getServerTime() - unit.Diff;

                            // Kick off the fast-forward. Need to run this before pushing the commands so the _animator.Play can take
                            animator.Update(deltaTime);

                            // Pump input into streams
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);
                            }

                            // Resimulate to present
                            var resimulationTime = deltaTime; // Skip the first update
                            while (resimulationTime < unit.Diff) {
                                animator.Update(Mathf.Min(deltaTime, unit.Diff - resimulationTime));
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                            }

                            State = RollbackState.Complete;
                        } else {
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);
                            }
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}