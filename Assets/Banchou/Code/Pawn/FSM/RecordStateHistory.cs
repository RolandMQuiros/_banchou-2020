using System;
using System.Linq;
using UnityEngine;
using UniRx;

using Banchou.Network;

namespace Banchou.Pawn.FSM {
    public class RecordStateHistory : FSMBehaviour {
        public void Construct(
            IObservable<GameState> observeState,
            Animator stateMachine,
            Part.Rollback rollback,
            GetServerTime getServerTime
        ) {
            void Dispatch(FSMUnit fsmUnit, float now) {
                var clip = stateMachine
                    .GetCurrentAnimatorClipInfo(fsmUnit.LayerIndex)
                    .OrderByDescending(c => c.weight)
                    .FirstOrDefault()
                    .clip;

                rollback.PushStateChange(
                    fsmUnit.StateInfo.fullPathHash,
                    clip?.isLooping ?? true,
                    (clip?.averageDuration ?? 1f) / fsmUnit.StateInfo.speed,
                    now
                );
            }

            var fastForwardFrames = 0f;
            ObserveStateUpdate
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    if (rollback.State == PawnRollbackState.FastForward) {
                        fastForwardFrames += Time.fixedUnscaledDeltaTime;
                    } else {
                        fastForwardFrames = 0f;
                    }
                })
                .AddTo(this);

            ObserveStateEnter
                .Select(fsmUnit => {
                    switch (rollback.State) {
                        case PawnRollbackState.Complete:
                            return (fsmUnit, getServerTime());
                        case PawnRollbackState.FastForward:
                            return (fsmUnit, rollback.FastForwardCurrentTime);
                    }
                    return (fsmUnit, -1f);
                })
                .Subscribe(args => {
                    var (fsmUnit, now) = args;
                    if (now >= 0f) {
                        Dispatch(fsmUnit, now);
                    }
                })
                .AddTo(this);

            var startingState = stateMachine.GetCurrentAnimatorStateInfo(0);
            rollback.PushStateChange(startingState.fullPathHash, startingState.loop, startingState.length, getServerTime());
        }
    }
}