using System;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UniRx.Triggers;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class Rewind : MonoBehaviour {
        [SerializeField, Tooltip("How much state history to record, in seconds")]
        private float _historyWindow = 2f;
        [SerializeField, Tooltip("Minimum delay between the current time and an input's timestamp before kicking off a rollback")]
        private float _rollbackThreshold = 0.15f;
        public enum RollbackState : byte {
            Complete,
            RollingBack,
            FastForward
        }

        private struct HistoryStep {
            public List<int> StateHashes;
            public List<float> NormalizedTimes;
            public Dictionary<int, float> Floats;
            public Dictionary<int, bool> Bools;
            public Dictionary<int, int> Ints;
            public Vector3 Position;
            public Vector3 Forward;
            public float When;
        }

        public void Construct(
            GetServerTime getServerTime,
            Animator animator,
            PawnId pawnId,
            IPawnInstance pawn,
            IMotor motor,
            Orientation orientation,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInput
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

            var movesAndCommands = playerInput
                .Where(unit => unit.Type != InputUnitType.Look);

            var observeCanRollback = observeState
                .Select(state => state.GetPawnPlayer(pawnId))
                .DistinctUntilChanged()
                .Select(state => state.RollbackEnabled);

            // TODO: Add redux state changes to this. Need a timestamp on Pawn
            // Pretend this was the idea the whole time
            var rollbackInputs = playerInput
                .Where(unit => unit.Type != InputUnitType.Look)
                .Where(unit => unit.When < getServerTime() - _rollbackThreshold)
                .BatchFrame(0, FrameCountType.FixedUpdate);

            // Record Animator's state every frame
            var history = new LinkedList<HistoryStep>();
            _gizmoFrames = history;
            HistoryStep RecordStep(float when) {
                return new HistoryStep {
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

            // Populate history list every frame
            observeCanRollback
                .Where(canRollback => canRollback)
                .SelectMany(_ => this.FixedUpdateAsObservable())
                .Select(xform => RecordStep(getServerTime()))
                .Subscribe(step => {
                    var window = getServerTime() - _historyWindow;

                    // Remove old frames
                    while (history.Count > 0 && history.First.Value.When < window) {
                        history.RemoveFirst();
                    }

                    history.AddLast(step);
                })
                .AddTo(this);

            // Handle rollbacks
            rollbackInputs
                .Subscribe(units => {
                    var deltaTime = Time.fixedUnscaledDeltaTime;
                    var now = getServerTime();
                    var correctionTime = units.Min(unit => unit.When);

                    // Find the last recorded frame before the input's timestamp, while removing future frames
                    var frame = history.Last.Value;
                    while (correctionTime < frame.When) {
                        history.RemoveLast();
                        frame = history.Last.Value;
                    }

                    _gizmoStep = frame;

                    // Set transform
                    motor.Clear();
                    motor.Teleport(frame.Position);
                    orientation.TrackForward(frame.Forward);

                    // Set animator states
                    animator.enabled = false;
                    // Physics.autoSimulation = false;

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
                    animator.enabled = true;

                    // Rollback will take it from here
                })
                .AddTo(this);
        }

        private LinkedList<HistoryStep> _gizmoFrames;
        private LinkedListNode<HistoryStep> _gizmoFastForwardStart;
        private LinkedListNode<HistoryStep> _gizmoFastForwardEnd;
        private HistoryStep _gizmoStep;

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