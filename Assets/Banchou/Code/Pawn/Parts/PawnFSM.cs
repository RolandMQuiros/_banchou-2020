using System;
using System.Linq;
using UnityEngine;
using UniRx;

using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class PawnFSM : MonoBehaviour {
        public void Construct(
            Animator stateMachine,
            PawnId pawnId,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInput
        ) {
            var observeStateHistory = observeState
                .Select(state => state.GetPawn(pawnId).FSMState)
                .DistinctUntilChanged()
                .Buffer(TimeSpan.FromSeconds(1));

            // Handle rollbacks
            observeState
                .Select(state => state.GetPawnPlayerId(pawnId))
                .DistinctUntilChanged()
                .SelectMany(playerId => playerInput.ObserveCommand(playerId))
                .Select(unit => (
                    Command: unit.Command,
                    When: unit.When,
                    Diff: Time.fixedUnscaledTime - unit.When
                ))
                .Where(unit => unit.Diff > 0f && unit.Diff < 1f)
                .WithLatestFrom(
                    observeStateHistory,
                    (unit, history) => (unit, history)
                )
                .Subscribe(args => {
                    var (unit, history) = args;

                    var targetState = history.Aggregate((target, step) => {
                        if (unit.When > step.FixedTimeAtChange) {
                            return step;
                        }
                        return target;
                    });

                    // Revert to state when desync happened
                    stateMachine.Play(targetState.StateHash, 0, targetState.ClipLength);

                    // Pump command into a stream somewhere

                    // Resimulate to present
                    stateMachine.playableGraph.Evaluate(unit.Diff);

                })
                .AddTo(this);
        }
    }
}