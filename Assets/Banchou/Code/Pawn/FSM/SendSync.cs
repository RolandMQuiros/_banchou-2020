using System;
using System.Linq;
using System.Collections.Generic;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Board;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class SendSync : FSMBehaviour {
        [SerializeField] private bool _sendOnEnter = false;
        [SerializeField] private bool _sendOnExit = false;

        [SerializeField] private bool _sendOnInterval = false;
        [SerializeField] private float _sendInterval = 0f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            BoardActions boardActions,
            Animator animator,
            IMotor motor,
            Orientation orientation,
            GetTime getTime,
            GetDeltaTime getDeltaTime
        ) {
            List<int> GetParameterKeys(AnimatorControllerParameterType parameterType) {
                return animator.parameters
                    .Where(p => p.type == parameterType)
                    .Select(p => p.nameHash)
                    .ToList();
            }
            var floatKeys = GetParameterKeys(AnimatorControllerParameterType.Float);
            var intKeys = GetParameterKeys(AnimatorControllerParameterType.Int);
            var boolKeys = GetParameterKeys(AnimatorControllerParameterType.Bool);

            // Choose the current or next animation states for each layer
            var selectStateInfo = Enumerable.Range(0, animator.layerCount)
                .Select(
                    layer => {
                        var current = animator.GetCurrentAnimatorStateInfo(layer);
                        var next = animator.GetNextAnimatorStateInfo(layer);
                        return animator.IsInTransition(layer) ? next : current;
                    }
                );

            PawnFrameData RecordFrame(float when) {
                return new PawnFrameData {
                    PawnId = pawnId,
                    StateHashes = selectStateInfo
                        .Select(stateInfo => stateInfo.fullPathHash)
                        .ToList(),
                    NormalizedTimes = selectStateInfo
                        .Select(stateInfo => stateInfo.normalizedTime % 1f)
                        .ToList(),
                    Floats = floatKeys.ToDictionary(key => key, key => animator.GetFloat(key)),
                    Ints = intKeys.ToDictionary(key => key, key => animator.GetInteger(key)),
                    Bools = boolKeys.ToDictionary(key => key, key => animator.GetBool(key)),
                    Position = motor.TargetPosition,
                    Forward = orientation.transform.forward,
                    When = when
                };
            }

            var observeIsServer = observeState
                .Select(state => state.IsServer())
                .DistinctUntilChanged()
                .Where(isServer => isServer);

            if (_sendOnEnter) {
                observeIsServer
                    .SelectMany(_ => ObserveStateEnter)
                    .CatchIgnoreLog()
                    .Subscribe(_ => {
                        dispatch(boardActions.SyncPawn(RecordFrame(getTime())));
                    })
                    .AddTo(this);
            }

            if (_sendOnExit) {
                observeIsServer
                    .SelectMany(_ => ObserveStateExit)
                    .CatchIgnoreLog()
                    .Subscribe(_ => {
                        dispatch(boardActions.SyncPawn(RecordFrame(getTime())));
                    })
                    .AddTo(this);
            }

            if (_sendOnInterval) {
                var stateTime = 0f;
                observeIsServer
                    .SelectMany(_ => ObserveStateEnter.Merge(ObserveStateExit))
                    .CatchIgnore()
                    .Subscribe(_ => {
                        stateTime = 0f;
                    })
                    .AddTo(this);

                observeIsServer
                    .SelectMany(_ => ObserveStateUpdate)
                    .CatchIgnore()
                    .Subscribe(_ => {
                        stateTime += getDeltaTime();
                        if (stateTime > _sendInterval) {
                            dispatch(boardActions.SyncPawn(RecordFrame(getTime())));
                            stateTime = 0f;
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}