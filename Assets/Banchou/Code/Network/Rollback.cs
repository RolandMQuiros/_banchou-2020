using System;
using System.Linq;
using System.Collections.Generic;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Board;
using Banchou.Player;

namespace Banchou.Network {
    public class Rollback : MonoBehaviour, IRollbackEvents {
        public RollbackPhase Phase { get; private set; } = RollbackPhase.Complete;
        public float CorrectionTime { get; private set; } = 0f;

        public IObservable<RollbackUnit> OnResimulationStart => _onResimulationStart;
        public Subject<RollbackUnit> _onResimulationStart = new Subject<RollbackUnit>();

        public IObservable<RollbackUnit> BeforeResimulateStep => _beforeResimulate;
        private Subject<RollbackUnit> _beforeResimulate = new Subject<RollbackUnit>();

        public IObservable<RollbackUnit> AfterResimulateStep => _afterResimulate;
        private Subject<RollbackUnit> _afterResimulate = new Subject<RollbackUnit>();

        public IObservable<RollbackUnit> OnResimulationEnd => _onResimulationEnd;
        public Subject<RollbackUnit> _onResimulationEnd = new Subject<RollbackUnit>();

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

            // Build a history list from state changes that don't incur rollbacks
            observeState
                .Where(state => state.IsRollbackEnabled())
                .Where(state => getServerTime() - state.GetBoardLastUpdated() <= state.GetRollbackDetectionThreshold())
                .Select(state => (HistoryDuration: state.GetRollbackHistoryDuration(), Board: state.GetBoard()))
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

            // Find state changes that require rollbacks
            var rollbackStateChanges = observeState
                .Where(state => state.IsRollbackEnabled())
                .Where(state => getServerTime() - state.GetBoardLastUpdated() > state.GetRollbackDetectionThreshold())
                .DistinctUntilChanged()
                .Select(state => new RollbackUnit {
                    TargetState = state.GetBoard(),
                    When = getServerTime(),
                    CorrectionTime = state.GetBoardLastUpdated(),
                    DeltaTime = deltaTime
                });

            // Find inputs that incur rollbacks
            var rollbackInputs = playerInput
                .Where(_ => getState().IsRollbackEnabled() && Phase == RollbackPhase.Complete)
                .Where(unit => unit.Type != InputUnitType.Look)
                .Where(unit => unit.When < getServerTime())
                .BatchFrame(0, FrameCountType.FixedUpdate)
                .Select(units => new RollbackUnit {
                    InputUnits = units,
                    When = getServerTime(),
                    CorrectionTime = Snapping.Snap(units.Min(unit => unit.When), deltaTime),
                    DeltaTime = deltaTime
                });

            var rollbacks = rollbackStateChanges.Merge(rollbackInputs);

            rollbacks
                .CatchIgnoreLog()
                .Subscribe(unit => {
                    var now = getServerTime();
                    CorrectionTime = unit.CorrectionTime;

                    // Disable physics tick
                    Physics.autoSimulation = false;

                    // Rewind the Board state
                    Phase = RollbackPhase.Rewind;
                    var step = history.Last.Value;
                    while (history.Count > 1 && step.When > CorrectionTime) {
                        history.RemoveLast();
                        step = history.Last.Value;
                    }

                    // Set the board state
                    dispatch(boardActions.Rollback(unit.TargetState ?? step.State));

                    RollbackUnit BuildRollbackUnit() => new RollbackUnit {
                        InputUnits = unit.InputUnits,
                        TargetState = getState().GetBoard(),
                        When = now,
                        CorrectionTime = CorrectionTime,
                        DeltaTime = deltaTime
                    };

                    void ResimulateStep() {
                        _beforeResimulate.OnNext(BuildRollbackUnit());
                        // Physics.Simulate(deltaTime);
                        _afterResimulate.OnNext(BuildRollbackUnit());

                        CorrectionTime += deltaTime;
                    }

                    _onResimulationStart.OnNext(BuildRollbackUnit());

                    // Run one frame so the animators can process inputs
                    ResimulateStep();

                    // Pump inputs back into the stream
                    for (int i = 0; unit.InputUnits != null && i < unit.InputUnits.Count; i++) {
                        playerInput.Push(unit.InputUnits[i]);
                    }

                    // Resimulate physics to present
                    while (CorrectionTime < now) {
                        ResimulateStep();
                    }

                    // Enable physics tick
                    Physics.autoSimulation = true;
                    Phase = RollbackPhase.Complete;

                    _onResimulationEnd.OnNext(BuildRollbackUnit());
                })
                .AddTo(this);
        }
    }
}