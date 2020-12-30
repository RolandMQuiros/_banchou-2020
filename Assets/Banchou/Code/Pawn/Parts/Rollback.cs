using System;
using System.Collections.Generic;
using System.Linq;

using Redux;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;
using Banchou.Network.Message;

namespace Banchou.Pawn.Part {
    public class Rollback : MonoBehaviour {
        [SerializeField, Tooltip("How much state history to record, in seconds")]
        private float _historyWindow = 2f;
        [SerializeField, Tooltip("Minimum delay between the current time and an input's timestamp before kicking off a rollback")]
        private float _rollbackThreshold = 0.15f;
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
                    )
                    .BatchFrame(1, FrameCountType.FixedUpdate)
                    .SelectMany(units => units.OrderBy(unit => unit.When));

                // Aggregate FSM changes into a list
                var fsmHistory = new LinkedList<PawnFSMState>();
                var observeFSMChanges = observeState
                    .Select(state => state.GetLatestFSMChange())
                    .DistinctUntilChanged()
                    .Where(change => change.PawnId == pawnId && change.StateHash != 0)
                    .DistinctUntilChanged(s => s.StateHash);

                observeFSMChanges
                    .Subscribe(fsmState => {
                        // Always have at least one state change on the list, regardless of how old it is
                        while (fsmHistory.Count > 1 && fsmHistory.First.Value.When < getServerTime() - _historyWindow) {
                            fsmHistory.RemoveFirst();
                            if (fsmHistory.Count == 0) {
                                Debug.LogError("HOW THE FUCK DID YOU DO THIS");
                            }
                        }
                        fsmHistory.AddLast(fsmState);
                    })
                    .AddTo(this);

                var xformHistory = new LinkedList<(Vector3 Position, Vector3 Forward, float When)>();
                this.FixedUpdateAsObservable()
                    .Where(_ => State == RollbackState.Complete)
                    .Subscribe(_ => {
                        var now = getServerTime();

                        while (xformHistory.Count > 1 && xformHistory.First.Value.When < now - _historyWindow) {
                            xformHistory.RemoveFirst();
                        }

                        var last = fsmHistory.LastOrDefault();
                        if (last == null || last.Position != pawn.Position || last.Forward != pawn.Forward) {
                            xformHistory.AddLast((
                                Position: pawn.Position,
                                Forward: pawn.Forward,
                                When: now
                            ));
                        }
                    })
                    .AddTo(this);

                // Handle rollbacks
                movesAndCommands
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        if (unit.Diff > _rollbackThreshold && State == RollbackState.Complete) {
                            var now = getServerTime();

                            Debug.Log(
                                "Rollback on unit:\n" +
                                $"\tCommand: {unit.Command}\n" +
                                $"\tMove: {unit.Move}\n" +
                                $"\tWhen: {unit.When}\n" +
                                $"\tDiff: {unit.Diff}\n" +
                                $"\tNow: {getServerTime()}\n"
                            );

                            Debug.Log(
                                "FSM Rollback History:\n\t" +
                                string.Join(
                                    "\n\t",
                                    fsmHistory.Select(step => $"Hash: {step.StateHash}, Position: {step.PawnId}, When: {step.When}")
                                )
                            );

                            var deltaTime = Mathf.Min(unit.Diff, Time.fixedUnscaledDeltaTime);
                            var targetState = fsmHistory
                                .OrderByDescending(step => step.When)
                                .First(step => unit.When > step.When);

                            // Revert to state when desync happened
                            var timeSinceStateStart = now - targetState.When;
                            var targetNormalizedTime = timeSinceStateStart % targetState.ClipLength;

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            State = RollbackState.RollingBack;

                            // Reposition/rotate to where the pawn was at time of rollback
                            var targetXform = xformHistory
                                .FirstOrDefault(step => unit.When < step.When);

                            if (targetXform.When != 0f) {
                                Debug.Log("Transform History:\n\t" +
                                    string.Join(
                                        "\n\t",
                                        xformHistory.Select(
                                            x => $"{x.Position}, {x.Forward}, {x.When}"
                                        )
                                    )
                                );

                                Debug.Log($"Rolling back position\n({pawn.Position} at {now}) -> ({targetXform.Position} at {targetXform.When}) for input at {unit.When}");

                                pawn.Position = targetXform.Position;
                                pawn.Forward = targetXform.Forward;
                            }

                            // Rewind
                            // animator.enabled = false;
                            animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: targetNormalizedTime
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            State = RollbackState.FastForward;
                            CorrectionTime = getServerTime() - unit.Diff;

                            // Need to call this once so triggers can be set, for some reason
                            animator.Update(deltaTime);

                            // I'd like to add to the xformHistory after every call to Animator.Update, but the positions are only updated
                            // at the next FixedUpdate, and all at the same time.

                            // Pump input into streams
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);
                            }

                            // Resimulate to present
                            var resimulationTime = deltaTime; // Skip first one
                            while (resimulationTime < unit.Diff) {
                                animator.Update(Mathf.Min(deltaTime, unit.Diff - resimulationTime));
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                            }
                            // animator.enabled = true;

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