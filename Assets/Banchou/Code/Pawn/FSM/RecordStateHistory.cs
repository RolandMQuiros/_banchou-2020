﻿using System;
using System.Linq;
using UnityEngine;
using UniRx;
using Redux;

namespace Banchou.Pawn.FSM {
    public class RecordStateHistory : FSMBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            GetState getState,
            Animator stateMachine,
            Dispatcher dispatch,
            PawnActions pawnActions
        ) {
            void Dispatch(FSMUnit fsmUnit, float now) {
                var clip = stateMachine
                    .GetCurrentAnimatorClipInfo(fsmUnit.LayerIndex)
                    .OrderByDescending(c => c.weight)
                    .FirstOrDefault()
                    .clip;

                dispatch(
                    pawnActions.FSMStateChanged(
                        fsmUnit.StateInfo.fullPathHash,
                        (clip?.averageDuration ?? 1f) / fsmUnit.StateInfo.speed,
                        clip?.isLooping ?? true,
                        now
                    )
                );
            }

            var fastForwardFrames = 0f;
            var observeRollbackState = observeState
                .Select(state => state.GetPawn(pawnId))
                .DistinctUntilChanged(pawn => pawn?.RollbackState);

            observeRollbackState
                .Where(pawn => pawn?.RollbackState == PawnRollbackState.FastForward)
                .SelectMany(pawn => ObserveStateUpdate)
                .CatchIgnore((Exception error) => Debug.LogException(error))
                .Subscribe(pawn => {
                    fastForwardFrames += Time.fixedUnscaledDeltaTime;
                })
                .AddTo(Streams);

            observeRollbackState
                .Where(pawn => pawn?.RollbackState != PawnRollbackState.FastForward)
                .Subscribe(_ => {
                    fastForwardFrames = 0f;
                })
                .AddTo(Streams);

            ObserveStateEnter
                .Select(fsmUnit => {
                    var pawn = getState().GetPawn(pawnId);
                    switch (pawn.RollbackState) {
                        case PawnRollbackState.Complete:
                            return (fsmUnit, Time.fixedUnscaledTime);
                        case PawnRollbackState.FastForward:
                            return (fsmUnit, pawn.RollbackCorrectionTime + fastForwardFrames);
                    }
                    return (fsmUnit, -1f);
                })
                .Subscribe(args => {
                    var (fsmUnit, now) = args;
                    if (now >= 0f) {
                        Dispatch(fsmUnit, now);
                    }
                })
                .AddTo(Streams);
        }
    }
}