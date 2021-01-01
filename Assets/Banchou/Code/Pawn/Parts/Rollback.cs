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
            IMotor motor
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
            var movesAndCommands = observeState
                .Select(state => state.GetPawnPlayerId(pawnId))
                .DistinctUntilChanged()
                .SelectMany(
                    playerId => playerInput.ObserveCommand(playerId)
                        .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                        .Select(unit => new InputUnit {
                            Command = unit.Command,
                            When = unit.When,
                            Diff = getServerTime() - unit.When
                        })
                        .Merge(playerInput.ObserveMove(playerId)
                            .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                            .Select(unit => new InputUnit {
                                Move = unit.Move,
                                When = unit.When,
                                Diff = getServerTime() - unit.When
                            })
                        )
                );

            bool IsUnitEligibleForRollback(InputUnit unit) {
                return unit.Diff > _rollbackThreshold;
            }

            var rollbackInputs = movesAndCommands
                .Where(IsUnitEligibleForRollback);

            var passthroughInputs = movesAndCommands
                .Where(unit => !IsUnitEligibleForRollback(unit));

            // Record Animator's state every frame
            var history = new LinkedList<HistoryStep>();

            this.FixedUpdateAsObservable()
                .CombineLatest(
                    motor.History,
                    (_, move) => move
                )
                .Select(move => new HistoryStep {
                    StateHashes = Enumerable.Range(0, animator.layerCount)
                        .Select(layer => animator.GetCurrentAnimatorStateInfo(layer).fullPathHash)
                        .ToList(),
                    NormalizedTimes = Enumerable.Range(0, animator.layerCount)
                        .Select(layer => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime)
                        .ToList(),
                    Floats = floatKeys.ToDictionary(key => key, key => animator.GetFloat(key)),
                    Ints = intKeys.ToDictionary(key => key, key => animator.GetInteger(key)),
                    Bools = boolKeys.ToDictionary(key => key, key => animator.GetBool(key)),
                    Position = move.Position,
                    Forward = pawn.Forward,
                    When = getServerTime(),
                })
                .Subscribe(step => {
                    var window = getServerTime() - _historyWindow;
                    while (history.Count > 0 && history.First.Value.When < window) {
                        history.RemoveFirst();
                    }

                    history.AddLast(step);
                    _gizmoFrames = history;
                })
                .AddTo(this);

            // Handle rollbacks
            rollbackInputs
                .Subscribe(unit => {
                    if (history.Count > 0) {
                        Debug.Log(
                            $"{history.Count} history frames available, from {history.First.Value.When} to {history.Last.Value.When}\n\t" +
                            string.Join(
                                "\n\t",
                                history.Select(step => $"Position: {step.Position}, When: {step.When}")
                            )
                        );
                    }

                    Debug.Log(
                        $"Rolling back for Input at {getServerTime()}:\n"+
                        $"\tWhen: {unit.When}\n" +
                        $"\tDiff: {unit.Diff}\n" +
                        $"\tMove: {unit.Move}\n" +
                        $"\tCommand: {unit.Command}"
                    );

                    var frame = history.First(step => unit.When <= step.When);
                    _gizmoStep = frame;

                    Debug.Log(
                        $"Target frame at {frame.When}:\n"+
                        $"\tFrom {pawn.Position} to {frame.Position}"
                    );

                    var now = getServerTime();
                    var deltaTime = Time.fixedUnscaledDeltaTime;

                    // Delete history after the target frame
                    while (history.Last.Value.When > frame.When) {
                        history.RemoveLast();
                    }

                    State = RollbackState.RollingBack;

                    // Set transform
                    motor.Teleport(frame.Position);
                    pawn.Forward = frame.Forward;

                    // Set animator states
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

                    CorrectionTime = now - unit.Diff;

                    // I'd like to add to the xformHistory after every call to Animator.Update, but the positions are only updated
                    // at the next FixedUpdate, and all at the same time.
                        // We absolutely need this is we want to stack rollbacks. Subsequent rollbacks can't use the local history, since it's invalidated
                        // by the preceding rollback.
                            // Hopefully recording on animator.OnUpdateAsObservable does something for us
                                // It doesn't

                    // Need to call this once so triggers can be set, for some reason
                    State = RollbackState.FastForward;
                    animator.Update(deltaTime);

                    // Pump input into streams
                    if (unit.Command == InputCommand.None) {
                        moveSubject.OnNext(unit.Move);
                    } else {
                        commandSubject.OnNext(unit.Command);
                    }

                    // Resimulate to present
                    var resimulationTime = deltaTime; // Skip first update
                    while (resimulationTime < unit.Diff) {
                        animator.Update(Mathf.Min(deltaTime, unit.Diff - resimulationTime));
                        resimulationTime = resimulationTime + deltaTime;
                    }
                    Debug.Log("Simulation ending");
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
        private HistoryStep _gizmoStep;

        private void OnDrawGizmos() {
            if (_gizmoFrames != null && _gizmoFrames.Count > 0) {
                Gizmos.color = Color.green;

                var iter = _gizmoFrames.First;
                while (iter.Next != null) {
                    Gizmos.DrawLine(iter.Value.Position, iter.Next.Value.Position);
                    Gizmos.DrawSphere(iter.Value.Position, 0.15f);
                    iter = iter.Next;
                }
            }

            if (_gizmoStep.When != 0f) {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_gizmoStep.Position, 0.25f);
            }
        }
    }
}