using System;
using System.Linq;

using UniRx;
using UnityEngine;

using Banchou.Combatant;
using Banchou.Player;
using Banchou.Pawn.Part;

namespace Banchou.Pawn.FSM {
    public class RotateToClosestTarget : FSMBehaviour {
        private enum Guide {
            ByOrientation,
            ByInput
        }

        [SerializeField] private Guide _guide = Guide.ByOrientation;
        [SerializeField] private float _maxTargetDistance = 4f;
        [SerializeField] private float _targetingPrecision = 0.4f;
        [SerializeField] private float _rotationSpeed = 1000f;
        [SerializeField] private bool _snapOnExit = false;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will start rotating to input")]
        private float _startTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will stop rotating to input")]
        private float _endTime = 1f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            ObservePlayerMove observePlayerMove,
            Rigidbody body,
            IPawnInstances pawnInstances,
            Orientation orientation = null
        ) {
            Vector3 faceDirection = Vector3.zero;

            // TODO: Appears to be evaluating the state multiple times. Change this to just save to some local vars in a subscription.
            var chooseTargetOnEnter = ObserveStateEnter
                .WithLatestFrom(
                    observeState
                        .Select(state => state.GetCombatantTargets(pawnId))
                        .DistinctUntilChanged(),
                    (_, targets) => targets
                )
                .WithLatestFrom(
                    observePlayerMove(), (targets, move) => (targets, move)
                )
                .Select(
                    substate => pawnInstances.GetMany(substate.targets)
                        .Where(
                            instance => {
                                if (instance != null) {
                                    var diff = instance.Position - body.transform.position;

                                    Vector3 basis = Vector3.zero;
                                    switch (_guide) {
                                        case Guide.ByInput:
                                            basis = substate.move;
                                            break;
                                        case Guide.ByOrientation:
                                            basis = orientation?.transform.forward ?? Vector3.zero;
                                            break;
                                    }

                                    return diff.magnitude < _maxTargetDistance &&
                                        Vector3.Dot(diff.normalized, basis) > _targetingPrecision;
                                }
                                return false;
                            }
                        )
                        .OrderBy(t => (t.Position - body.transform.position).sqrMagnitude)
                        .FirstOrDefault()
                );

            ObserveStateUpdate
                .Select(unit => unit.StateInfo.normalizedTime % 1)
                .Where(time => time >= _startTime && time <= _endTime)
                .WithLatestFrom(chooseTargetOnEnter, (_, target) => target)
                .Where(target => target != null)
                .Select(
                    t => Vector3.ProjectOnPlane(
                        t.Position - body.transform.position,
                        body.transform.up
                    ).normalized
                )
                .CatchIgnoreLog()
                .Subscribe(targetDirection => {
                    faceDirection = targetDirection;
                    orientation.transform.rotation = Quaternion.RotateTowards(
                        orientation.transform.rotation,
                        Quaternion.LookRotation(targetDirection),
                        _rotationSpeed * Time.fixedDeltaTime
                    );
                })
                .AddTo(this);

            if (_snapOnExit) {
                ObserveStateExit
                    .Subscribe(_ => {
                        // Snap to the facing direction on state exit.
                        // Helps face the character in the intended direction when jumping mid-turn.
                        if (faceDirection != Vector3.zero) {
                            orientation.transform.rotation = Quaternion.LookRotation(faceDirection);
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}