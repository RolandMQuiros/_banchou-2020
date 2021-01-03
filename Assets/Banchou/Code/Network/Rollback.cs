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
        [SerializeField, Tooltip("How much state history to record, in seconds")]
        private float _historyWindow = 2f;
        [SerializeField, Tooltip("Minimum delay between the current time and an input's timestamp before kicking off a rollback")]
        private float _rollbackThreshold = 0.15f;

        public enum RollbackPhase {
            Complete,
            Rewind,
            Resimulate
        }
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
            PlayerInputStreams playerInput
        ) {
            var history = new LinkedList<HistoryStep>();
            var deltaTime = Time.fixedUnscaledDeltaTime; // Find out where the target framerate is

            observeState
                .Select(state => state.Board)
                .DistinctUntilChanged()
                .Subscribe(board => {
                    while (history.Count > 0 && history.First.Value.When < getServerTime() - _historyWindow) {
                        history.RemoveFirst();
                    }

                    history.AddLast(new HistoryStep {
                        State = board,
                        When = getServerTime()
                    });
                })
                .AddTo(this);

            var rollbackInputs = playerInput
                .Where(unit => unit.Type != InputUnitType.Look)
                .Where(unit => unit.When < getServerTime() - _rollbackThreshold)
                .BatchFrame(); // Batch at the end of frame, so the Pawns have time to rewind themselves

            rollbackInputs
                .Subscribe(units => {
                    var now = getServerTime();
                    CorrectionTime = units.Min(unit => unit.When);

                    // Disable physics tick
                    Physics.autoSimulation = false;

                    // Rewind the Board state
                    Phase = RollbackPhase.Rewind;
                    var step = history.Last.Value;
                    while (step.When > CorrectionTime) {
                        history.RemoveLast();
                        step = history.Last.Value;
                    }

                    // Reapply inputs
                    for (int i = 0; i < units.Count; i++) {
                        var unit = units[i];
                        playerInput.Push(new InputUnit(unit) {
                            When = CorrectionTime
                        });
                    }

                    // Resimulate physics to present
                    Phase = RollbackPhase.Resimulate;
                    while (CorrectionTime < now) {
                        Physics.Simulate(deltaTime);
                        CorrectionTime += deltaTime;
                    }

                    // Enable physics tick
                    Phase = RollbackPhase.Complete;
                    Physics.autoSimulation = true;
                })
                .AddTo(this);
        }
    }
}