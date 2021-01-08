using System;
using System.Linq;
using System.Collections.Generic;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Board;
using Banchou.Player;

namespace Banchou.Network {
    public class Rollback : IDisposable, IRollbackEvents {
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

        private CompositeDisposable _subscriptions;

        private struct HistoryStep {
            public BoardState State;
            public float When;
        }

        public Rollback(
            IObservable<GameState> observeState,
            IObservable<RemoteAction> observeRemoteActions,
            IObservable<InputUnit> observeRemoteInput,

            Dispatcher dispatch,
            GetServerTime getServerTime,
            PlayerInputStreams playerInput,
            BoardActions boardActions
        ) {
            var history = new LinkedList<HistoryStep>();
            var deltaTime = Time.fixedUnscaledDeltaTime; // Find out where the target framerate is

            var rollbackSettings = observeState
                .Select(state => (
                    IsEnabled: state.IsRollbackEnabled(),
                    Threshold: state.GetRollbackDetectionThreshold()
                ))
                .DistinctUntilChanged();

            // Find remote actions that incur rollbacks
            var rollbackActions = rollbackSettings
                .Where(state => state.IsEnabled)
                .SelectMany(
                    state => observeRemoteActions
                        .Where(_ => history.Count > 0)
                        .Where(action => getServerTime() - action.When > state.Threshold)
                )
                .Select(
                    remoteAction => new RollbackUnit {
                        Action = remoteAction.Action,
                        When = getServerTime(),
                        CorrectionTime = remoteAction.When,
                        DeltaTime = deltaTime
                    }
                );

            // Actions that don't require rollback
            var passthroughActions = rollbackSettings
                .SelectMany(
                    state => observeRemoteActions
                        .Where(action => !state.IsEnabled ||
                            history.Count <= 0 ||
                            getServerTime() - action.When <= state.Threshold)
                );

            // Find inputs that incur rollbacks
            var rollbackInputs = rollbackSettings
                .Where(state => state.IsEnabled)
                .SelectMany(state => observeRemoteInput
                    .Where(_ => history.Count > 0)
                    .Where(unit => getServerTime() - unit.When < state.Threshold)
                )
                .Where(_ => Phase == RollbackPhase.Complete)
                .BatchFrame(0, FrameCountType.FixedUpdate)
                .Select(units => new RollbackUnit {
                    InputUnits = units,
                    When = getServerTime(),
                    CorrectionTime = Snapping.Snap(units.Min(unit => unit.When), deltaTime),
                    DeltaTime = deltaTime
                });

            // Inputs that don't require rollback
            var passthroughInputs = rollbackSettings
                .SelectMany(state => observeRemoteInput
                    .Where(unit => !state.IsEnabled ||
                        history.Count <= 0 ||
                        getServerTime() - unit.When >= state.Threshold)
                )
                .Do(unit => Debug.Log($"Passthrough input: {unit.Direction}"));

            // All rollback events
            var rollbacks = rollbackActions.Merge(rollbackInputs);

            _subscriptions = new CompositeDisposable(
                // Build a history list from state changes that don't incur rollbacks
                observeState
                    .Select(state => (
                        IsEnabled: state.IsRollbackEnabled(),
                        LastUpdated: state.GetBoardLastUpdated(),
                        Threshold: state.GetRollbackDetectionThreshold(),
                        HistoryDuration: state.GetRollbackHistoryDuration(),
                        Board: state.GetBoard()
                    ))
                    .DistinctUntilChanged()
                    .Where(state => state.IsEnabled)
                    .Where(state => getServerTime() - state.LastUpdated <= state.Threshold)
                    .Subscribe(args => {
                        while (history.Count > 1 && history.First.Value.When < getServerTime() - args.HistoryDuration) {
                            history.RemoveFirst();
                        }

                        history.AddLast(new HistoryStep {
                            State = args.Board,
                            When = getServerTime()
                        });
                    }),
                // Handle rollbacks
                rollbacks
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        var now = getServerTime();
                        CorrectionTime = unit.CorrectionTime;

                        // Disable physics tick
                        Physics.autoSimulation = false;

                        // Rewind the Board state
                        Phase = RollbackPhase.Rewind;

                        // Find where in history we're rewinding to, removing all invalid future states while we do
                        var step = history.Last.Value;
                        while (history.Count > 1 && step.When > CorrectionTime) {
                            history.RemoveLast();
                            step = history.Last.Value;
                        }

                        // Set the board state
                        dispatch(boardActions.Rollback(step.State));

                        void ResimulateStep() {
                            _beforeResimulate.OnNext(unit);
                            // Physics.Simulate(deltaTime);
                            _afterResimulate.OnNext(unit);
                            unit.CorrectionTime += CorrectionTime += deltaTime;
                        }
                        _onResimulationStart.OnNext(unit);

                        // Run one frame so the animators can process inputs
                        ResimulateStep();

                        // Dispatch deferred action
                        if (unit.Action != null) {
                            dispatch(unit.Action);
                        }

                        // Pump inputs into the stream
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

                        _onResimulationEnd.OnNext(unit);
                    }),
                // Handle passthroughs
                passthroughActions
                    .CatchIgnoreLog()
                    .Subscribe(remoteAction => {
                        dispatch(remoteAction.Action);
                    }),
                passthroughInputs
                    .CatchIgnoreLog()
                    .Subscribe(inputUnit => {
                        playerInput.Push(inputUnit);
                    })
            );
        }

        public void Dispose() {
            _subscriptions.Dispose();
        }
    }
}