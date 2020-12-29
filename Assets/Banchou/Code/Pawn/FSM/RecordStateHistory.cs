using System;
using System.Linq;
using UnityEngine;
using UniRx;
using Redux;

using Banchou.Network;

namespace Banchou.Pawn.FSM {
    public class RecordStateHistory : FSMBehaviour {
        public void Construct(
            PawnId pawnId,
            IPawnInstance pawn,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PawnActions pawnActions,
            Animator stateMachine,
            GetServerTime getServerTime,
            Part.Rollback rollback
        ) {
            void FSMStateChanged(FSMUnit fsmUnit, float now) {
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
                        now,
                        pawn.Position,
                        pawn.Forward
                    )
                );
            }

            var fastForwardFrames = 0f;

            ObserveStateUpdate
                .Subscribe(unit => {
                    switch (rollback.State) {
                        case Part.Rollback.RollbackState.FastForward:
                            fastForwardFrames += Time.fixedUnscaledDeltaTime;
                            break;
                        default:
                            fastForwardFrames = 0f;
                            break;
                    }
                })
                .AddTo(this);

            ObserveStateEnter
                .Select(fsmUnit => {
                    switch (rollback.State) {
                        case Part.Rollback.RollbackState.Complete:
                            return (fsmUnit, getServerTime());
                        case Part.Rollback.RollbackState.FastForward:
                            return (fsmUnit, rollback.CorrectionTime + fastForwardFrames);
                        case Part.Rollback.RollbackState.RollingBack:
                        default:
                            return (fsmUnit, -1f);
                    }
                })
                .Subscribe(args => {
                    var (fsmUnit, now) = args;
                    if (now >= 0f) {
                        FSMStateChanged(fsmUnit, now);
                    }
                })
                .AddTo(this);

            var startingState = stateMachine.GetCurrentAnimatorStateInfo(0);
            FSMStateChanged(
                new FSMUnit {
                    StateInfo = startingState,
                    LayerIndex = 0
                },
                getServerTime()
            );
        }
    }
}