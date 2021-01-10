using System;
using System.Collections.Generic;
using System.Linq;

using Redux;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

using Banchou.Board;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class PawnRollback : MonoBehaviour {
        public void Construct(
            PawnId pawnId,
            IRollbackEvents rollback,
            GetTime getTime,
            Animator animator,
            IMotor motor,
            Orientation orientation,
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            BoardActions boardActions
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

            PawnFrameData RecordStep(float when) {
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

            // Record Animator's state every frame
            var history = new LinkedList<PawnFrameData>();
            history.AddLast(RecordStep(getTime()));

            _gizmoFrames = history;

            void SetAnimatorFrame(in PawnFrameData frame) {
                // Set transform
                motor.Clear();
                motor.Teleport(frame.Position);
                orientation.TrackForward(frame.Forward);

                // Set animator states
                animator.UseFrame(frame);
            }

            // Populate history list every frame
            this.FixedUpdateAsObservable()
                .Select(_ => RecordStep(getTime()))
                .Subscribe(step => {
                    var window = getTime() - getState().GetRollbackHistoryDuration();

                    // Remove old frames
                    while (history.Count > 1 && history.First.Value.When < window) {
                        history.RemoveFirst();
                    }

                    history.AddLast(step);
                })
                .AddTo(this);

            // Handle rollbacks
            rollback
                .OnResimulationStart
                .Subscribe(unit => {
                    var deltaTime = unit.DeltaTime;
                    var now = unit.When;
                    var correctionTime = unit.CorrectionTime;

                    // Find the last recorded frame before the input's timestamp, while removing future frames
                    var frame = history.Last.Value;
                    while (correctionTime < history.Last.Value.When) {
                        history.RemoveLast();
                        if (history.Count > 0) {
                            frame = history.Last.Value;
                        } else {
                            return;
                        }
                    }

                    animator.enabled = false;

                    _gizmoStep = frame;
                    SetAnimatorFrame(frame);
                    _gizmoFastForwardStart = history.Last;
                })
                .AddTo(this);

            rollback.BeforeResimulateStep
                .CatchIgnoreLog()
                .Subscribe(unit => {
                    animator.Update(unit.DeltaTime);
                    motor.Apply();
                })
                .AddTo(this);

            rollback.AfterResimulateStep
                .CatchIgnoreLog()
                .Subscribe(unit => {
                    history.AddLast(RecordStep(unit.CorrectionTime));
                    _gizmoFastForwardEnd = history.Last;
                })
                .AddTo(this);

            rollback.OnResimulationEnd
                .CatchIgnoreLog()
                .Subscribe(unit => {
                    animator.enabled = true;
                })
                .AddTo(this);
        }

        private LinkedList<PawnFrameData> _gizmoFrames;
        private LinkedListNode<PawnFrameData> _gizmoFastForwardStart;
        private LinkedListNode<PawnFrameData> _gizmoFastForwardEnd;
        private PawnFrameData _gizmoStep;

        private void OnDrawGizmos() {
            if (_gizmoFrames != null && _gizmoFrames.Count > 0) {
                var ageBounds = _gizmoFrames.Last.Value.When - _gizmoFrames.First.Value.When;
                var color = Color.green;

                var iter = _gizmoFrames.First;
                while (iter.Next != null) {
                    var age = (iter.Value.When - _gizmoFrames.First.Value.When) / ageBounds;
                    color.g = age;

                    Gizmos.color = color;
                    Gizmos.DrawLine(iter.Value.Position, iter.Next.Value.Position);
                    Gizmos.DrawSphere(iter.Value.Position, 0.15f * age);
                    iter = iter.Next;
                }

                if (_gizmoFastForwardStart != null && _gizmoFastForwardEnd != null) {
                    Gizmos.color = Color.magenta;
                    var ffIter = _gizmoFastForwardStart;
                    while (ffIter != null && ffIter != _gizmoFastForwardEnd) {
                        var age = (ffIter.Value.When - _gizmoFrames.First.Value.When) / ageBounds;
                        Gizmos.DrawWireSphere(ffIter.Value.Position, 0.2f * age);
                        ffIter = ffIter.Next;
                    }
                }
            }

            if (_gizmoStep.When != 0f) {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_gizmoStep.Position, 0.25f);
            }
        }
    }
}