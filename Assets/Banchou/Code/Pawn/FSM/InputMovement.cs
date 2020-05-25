using System;
using UnityEngine;
using UniRx;

using Banchou.Mob;

namespace Banchou.Pawn.FSM {
    public class InputMovement : FSMBehaviour {
        [Header("Movement")]
        [SerializeField, Tooltip("How quickly, in units per second, the object moves along its motion vector")]
        private float _movementSpeed = 8f;

        [Header("Animation")]
        [SerializeField] private float _acceleration = 1f;

        [Header("Animation Parameters")]
        [SerializeField, Tooltip("Animation parameter to write movement speed")]
        private string _movementSpeedOut = string.Empty;
        [SerializeField] private string _velocityRightOut = string.Empty;
        [SerializeField] private string _velocityForwardOut = string.Empty;


        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Part.IMotor motor,
            Part.Orientation orientation,
            Animator animator
        ) {
            var speedOut = Animator.StringToHash(_movementSpeedOut);
            var rightSpeedOut = Animator.StringToHash(_velocityRightOut);
            var forwardSpeedOut = Animator.StringToHash(_velocityForwardOut);
            var walkingCurve = 0f;

            ObserveStateUpdate
                .WithLatestFrom(observeState, (_, state) => state)
                .Where(state => !state.IsMobApproaching(pawnId))
                .Select(state => state.GetPawnPlayerInputMovement(pawnId))
                .Select(input => Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized * input.y +
                    Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized * input.x)
                .CatchIgnore((Exception error) => { Debug.LogException(error); })
                .Subscribe(direction => {
                    var velocity = _movementSpeed * direction;
                    motor.Move(velocity * Time.fixedDeltaTime);

                    // Write to output variables
                    if (!string.IsNullOrWhiteSpace(_movementSpeedOut)) {
                        if (direction == Vector3.zero) {
                            walkingCurve = Mathf.Clamp(walkingCurve - (_acceleration * Time.fixedDeltaTime), 0f, 1f);
                        } else {
                            walkingCurve = Mathf.Clamp(walkingCurve + (_acceleration * Time.fixedDeltaTime), 0f, 1f);
                        }

                        animator.SetFloat(speedOut, velocity.magnitude);
                        animator.SetFloat(rightSpeedOut, Vector3.Dot(direction, orientation.transform.right));
                        animator.SetFloat(forwardSpeedOut, Vector3.Dot(direction, orientation.transform.forward));
                    }
                })
                .AddTo(Streams);
        }
    }
}