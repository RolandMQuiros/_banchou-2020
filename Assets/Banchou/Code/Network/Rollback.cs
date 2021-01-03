using System;
using System.Linq;
using System.Collections.Generic;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Board;
using Banchou.Player;

namespace Banchou.Network {
    public class Rollback : MonoBehaviour {
        public RollbackPhase Phase { get; private set; } = RollbackPhase.Complete;
        public float CorrectionTime { get; private set; } = 0f;

        private struct HistoryStep {
            public BoardState State;
            public float When;
        }

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            GetServerTime getServerTime,
            PlayerInputStreams playerInput,
            BoardActions boardActions,
            GetState getState
        ) {
            var history = new LinkedList<HistoryStep>();
            var deltaTime = Time.fixedUnscaledDeltaTime; // Find out where the target framerate is

            observeState
                .Where(state => state.IsRollbackEnabled())
                .Select(state => (HistoryDuration: state.GetRollbackHistoryDuration(), state.Board))
                .DistinctUntilChanged()
                .Subscribe(args => {
                    while (history.Count > 1 && history.First.Value.When < getServerTime() - args.HistoryDuration) {
                        history.RemoveFirst();
                    }

                    history.AddLast(new HistoryStep {
                        State = args.Board,
                        When = getServerTime()
                    });
                })
                .AddTo(this);

            var rollbackInputs = playerInput
                .Where(_ => getState().IsRollbackEnabled() && Phase == RollbackPhase.Complete)
                .Where(unit => unit.Type != InputUnitType.Look)
                .Where(unit => unit.When < getServerTime() - getState().GetRollbackDetectionThreshold())
                .BatchFrame();

            rollbackInputs
                .Subscribe(units => {
                    var now = getServerTime();
                    CorrectionTime = units.Min(unit => unit.When);

                    Debug.Log(
                        $"Rolling back from {now} to {CorrectionTime}\n\t" +
                        string.Join(
                            "\n\t",
                            units.Select(unit => $"{unit.Type} at {unit.When} - Player: {unit.PlayerId}, Command: {unit.Command}, Direction: {unit.Direction}")
                        )
                    );

                    // Disable physics tick
                    Physics.autoSimulation = false;

                    // Rewind the Board state
                    Phase = RollbackPhase.Rewind;
                    var step = history.Last.Value;
                    while (step.When > CorrectionTime) {
                        history.RemoveLast();
                        step = history.Last.Value;
                    }

                    // Set the board state
                    dispatch(boardActions.Rollback(step.State));

                    // Run once so Animators can process input
                    Phase = RollbackPhase.Resimulate;
                    Physics.Simulate(deltaTime);
                    CorrectionTime += deltaTime;

                    // Reapply inputs
                    for (int i = 0; i < units.Count; i++) {
                        playerInput.Push(units[i]);
                    }

                    // Resimulate physics to present
                    while (CorrectionTime < now) {
                        Physics.Simulate(deltaTime);
                        CorrectionTime += deltaTime;
                    }

                    // Enable physics tick
                    Physics.autoSimulation = true;
                    Phase = RollbackPhase.Complete;
                })
                .AddTo(this);
        }
    }
}