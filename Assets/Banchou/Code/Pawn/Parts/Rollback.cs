using System;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UniRx.Triggers;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class Rollback : MonoBehaviour {
        [SerializeField, Tooltip("How much state history to record, in seconds")]
        private float _historyWindow = 2f;
        [SerializeField, Tooltip("Minimum delay between the current time and an input's timestamp before kicking off a rollback")]
        private float _rollbackThreshold = 0.15f;
        public enum RollbackState : byte {
            Complete,
            RollingBack,
            FastForward
        }
        public RollbackState State { get; private set; } = RollbackState.Complete;
        public float CorrectionTime { get; private set; } = 0f;

        private struct InputUnit {
            public PlayerId PlayerId;
            public InputCommand Command;
            public Vector3 Move;
            public float When;
            public float Diff;
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

            public override string ToString() {
                return "{\n" +
                    $"\tStateHashes: [{string.Join(", ", StateHashes)}],\n" +
                    $"\tNormalizedTimes: [{string.Join(", ", NormalizedTimes)}],\n" +
                    $"\tFloats: {{\n\t" +
                        string.Join(",\n\t", Floats.Select(p => $"{p.Key}: {p.Value}")) +
                    "],\n" +
                    $"\tBools: {{\n\t" +
                        string.Join(",\n\t", Bools.Select(p => $"{p.Key}: {p.Value}")) +
                    "],\n" +
                    $"\tInts: {{\n\t" +
                        string.Join(",\n\t", Ints.Select(p => $"{p.Key}: {p.Value}")) +
                    "],\n" +
                    $"\tPosition: {Position},\n" +
                    $"\tForward: {Forward},\n" +
                    $"\tWhen: {When}\n" +
                "}";
            }
        }

        public void Construct(
            GetServerTime getServerTime,
            Animator animator,
            PawnId pawnId,
            IPawnInstance pawn,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInput,
            Subject<InputCommand> commandSubject,
            Subject<Vector3> moveSubject,

            IMotor motor,
            Orientation orientation
        ) {
            var floatKeys = animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Float)
                .Select(p => p.nameHash)
                .ToList();
            var intKeys = animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Int)
                .Select(p => p.nameHash)
                .ToList();
            var boolKeys = animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Bool)
                .Select(p => p.nameHash)
                .ToList();
            var triggerKeys = animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Trigger)
                .Select(p => p.nameHash)
                .ToList();

            // Merge movements and commands into one stream
                // TODO: Add redux state changes to this. Need a timestamp on Pawn
                // Pretend this was the idea the whole time
            var movesAndCommands = playerInput.ObserveCommand()
                .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                .Select(unit => new InputUnit {
                    PlayerId = unit.PlayerId,
                    Command = unit.Command,
                    When = unit.When,
                    Diff = getServerTime() - unit.When
                })
                .Merge(playerInput.ObserveMove()
                    .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                    .Select(unit => new InputUnit {
                        PlayerId = unit.PlayerId,
                        Move = unit.Move,
                        When = unit.When,
                        Diff = getServerTime() - unit.When
                    })
                );

            bool IsUnitEligibleForRollback(InputUnit unit) {
                return unit.Diff > _rollbackThreshold;
            }

            var observeCanRollback = observeState
                .Select(state => state.GetPawnPlayer(pawnId))
                .DistinctUntilChanged()
                .Select(state => state.RollbackEnabled);

            var rollbackInputs = observeCanRollback
                .Where(canRollback => canRollback)
                .SelectMany(_ => movesAndCommands)
                .Where(IsUnitEligibleForRollback);

            var passthroughInputs = movesAndCommands
                .WithLatestFrom(observeCanRollback, (Unit, CanRollback) => (Unit, CanRollback))
                .Where(args => !args.CanRollback || !IsUnitEligibleForRollback(args.Unit))
                .Select(args => args.Unit);

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
            observeState
                .Select(state => state.GetPawnPlayerId(pawnId))
                .SelectMany(playerId => rollbackInputs.Select(unit => (unit, playerId)))
                .Subscribe(args => {
                    var (unit, playerId) = args;
                    var deltaTime = Time.fixedUnscaledDeltaTime;
                    var now = getServerTime();

                    // Find the last recorded frame before the input's timestamp, while removing future frames
                    while (unit.When < history.Last.Value.When) {
                        history.RemoveLast();
                    }

                    // Go back one more frame, since we need to play the animator once for input to process
                    var frame = history.Last.Previous.Value;
                    history.RemoveLast();

                    _gizmoStep = frame;

                    State = RollbackState.RollingBack;

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

                    State = RollbackState.FastForward;

                    // Resimulate to present
                    CorrectionTime = now - unit.Diff;

                    // Need to run this first or else triggers aren't set, for some reason
                    animator.Update(deltaTime);
                    // Physics.Simulate(deltaTime);
                    var resimulatedStep = RecordStep(CorrectionTime);
                    history.AddLast(resimulatedStep);
                    _gizmoFastForwardStart = history.Last;

                    // Pump input into streams
                    if (unit.PlayerId == playerId) {
                        if (unit.Command == InputCommand.None) {
                            moveSubject.OnNext(unit.Move);
                        } else {
                            commandSubject.OnNext(unit.Command);
                        }
                    }

                    CorrectionTime += deltaTime;

                    while (CorrectionTime < now) {
                        animator.Update(deltaTime);
                        // Record resimulated frame's new state
                        resimulatedStep = RecordStep(CorrectionTime);
                        // Physics.Simulate(deltaTime);
                        history.AddLast(resimulatedStep);

                        CorrectionTime += deltaTime;
                    }
                    _gizmoFastForwardEnd = history.Last;
                    animator.enabled = true;
                    // Physics.autoSimulation = true;
                })
                .AddTo(this);

            // Post-rollback afterglow
            this.LateUpdateAsObservable()
                .Where(_ => State == RollbackState.FastForward)
                .Subscribe(_ => {
                    State = RollbackState.Complete;
                    Debug.Log($"Rollback complated at position {pawn.Position} at {getServerTime()}");
                })
                .AddTo(this);

            // Passthrough non-rollback inputs
            passthroughInputs
                .Subscribe(unit => {
                    if (unit.Command == InputCommand.None) {
                        moveSubject.OnNext(unit.Move);
                    } else {
                        commandSubject.OnNext(unit.Command);
                    }
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