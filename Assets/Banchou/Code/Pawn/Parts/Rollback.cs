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
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PawnActions pawnActions,
            PlayerInputStreams playerInput,
            Subject<InputCommand> commandSubject,
            GetServerTime getServerTime,
            Animator animator = null
        ) {
            if (animator != null) {
                // Aggregate FSM changes into a list
                var history = new LinkedList<PawnFSMState>();
                observeState
                    .Select(state => state.GetLatestFSMChange())
                    .Where(change => change.PawnId == pawnId)
                    .Where(s => s.StateHash != 0)
                    .DistinctUntilChanged()
                    .Subscribe(fsmState => {
                        while (history.First?.Value.FixedTimeAtChange < getServerTime() - 0.5f) {
                            history.RemoveFirst();
                        }
                        // Always have at least one state change on the list
                        history.AddLast(fsmState);
                    })
                    .AddTo(this);

                // Handle rollbacks
                observeState
                    .Select(state => state.GetPawnPlayerId(pawnId))
                    .DistinctUntilChanged()
                    .SelectMany(
                        playerId => playerInput.ObserveCommand(playerId)
                            .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                            .Select(unit => (
                                Command: unit.Command,
                                When: unit.When,
                                Diff: getServerTime() - unit.When
                            ))
                    )
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        if (unit.Diff > 0f && unit.Diff < 1f) {
                            var now = getServerTime();
                            var deltaTime = Time.fixedUnscaledDeltaTime;

                            var targetState = history.Aggregate((target, step) => {
                                if (unit.When > step.FixedTimeAtChange) {
                                    return step;
                                }
                                return target;
                            });

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            dispatch(pawnActions.RollbackStarted());

                            // Revert to state when desync happened
                            animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: (now - targetState.FixedTimeAtChange - deltaTime)
                                    / targetState.ClipLength
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            dispatch(pawnActions.FastForwarding(unit.Diff));

                            // Kick off the fast-forward. Need to run this before pushing the commands so the _animator.Play can take
                            animator.Update(deltaTime);

                            // Pump command into a stream somewhere
                            commandSubject.OnNext(unit.Command);

                            // Resimulate to present
                            var resimulationTime = deltaTime; // Skip the first update
                            while (resimulationTime < unit.Diff) {
                                animator.Update(deltaTime);
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                            }
                            dispatch(pawnActions.RollbackComplete());
                        } else {
                            commandSubject.OnNext(unit.Command);
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}