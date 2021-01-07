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
        [SerializeField, Tooltip("Server only. Amount of time, in seconds, between sending synchronization messages to clients")]
        private float _syncInterval = 5f;

        public void Construct(
            PawnId pawnId,
            IRollbackEvents rollback,
            GetServerTime getServerTime,
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
            var triggerKeys = GetParameterKeys(AnimatorControllerParameterType.Trigger);

            // Record Animator's state every frame
            var history = new LinkedList<PawnFrameData>();
            _gizmoFrames = history;
            PawnFrameData RecordStep(float when) {
                return new PawnFrameData {
                    PawnId = pawnId,
                    StateHashes = Enumerable.Range(0, animator.layerCount)
                        .Select(layer => animator.GetCurrentAnimatorStateInfo(layer).fullPathHash)
                        .ToList(),
                    NormalizedTimes = Enumerable.Range(0, animator.layerCount)
                        .Select(layer => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime)
                        .ToList(),
                    Floats = floatKeys.ToDictionary(key => key, key => animator.GetFloat(key)),
                    Ints = intKeys.ToDictionary(key => key, key => animator.GetInteger(key)),
                    Bools = boolKeys.ToDictionary(key => key, key => animator.GetBool(key)),
                    Position = motor.TargetPosition,
                    Forward = orientation.transform.forward,
                    When = when,
                };
            }

            void SetAnimatorFrame(in PawnFrameData frame) {
                // Set transform
                motor.Clear();
                motor.Teleport(frame.Position);
                orientation.TrackForward(frame.Forward);

                // Set animator states
                animator.enabled = false;

                for (int layer = 0; layer < animator.layerCount; layer++) {
                    animator.Play(frame.StateHashes[layer], layer, frame.NormalizedTimes[layer]);
                }

                // Set animator parameters
                foreach (var param in frame.Floats) {
                    animator.SetFloat(param.Key, param.Value);
                }

                foreach (var param in frame.Ints) {
                    animator.SetInteger(param.Key, param.Value);
                }

                foreach (var param in frame.Bools) {
                    animator.SetBool(param.Key, param.Value);
                }

                // Reset triggers
                foreach (var param in triggerKeys) {
                    animator.ResetTrigger(param);
                }
            }

            // Populate history list every frame
            this.FixedUpdateAsObservable()
                .Select(_ => getServerTime())
                .Select(when => RecordStep(when))
                .Subscribe(step => {
                    var window = getServerTime() - getState().GetRollbackHistoryDuration();

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
                        // Go back one additional frame, since we need to run animators at least once before they accept inputs
                        frame = history.Last.Previous.Value;
                    }

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
                .Subscribe(_ => { animator.enabled = true; })
                .AddTo(this);

            // Send pawn syncs
            Observable.Interval(TimeSpan.FromSeconds(_syncInterval))
                .ThrottleFrame(0, FrameCountType.FixedUpdate)
                .CatchIgnore()
                .Subscribe(_ => {
                    dispatch(
                        boardActions.SyncPawn(RecordStep(getServerTime()))
                    );
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