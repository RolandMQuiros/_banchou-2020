using System;
using UnityEngine;
using UniRx;

using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class RotateToInput : FSMBehaviour {
        [SerializeField, Tooltip("How quickly, in degrees per second, the Object will rotate to face its motion vector")]
        private float _rotationSpeed = 1000f;
        [SerializeField, Tooltip("If enabled, immediately snaps rotation to input on state exit")]
        private bool _snapOnExit = true;

        [SerializeField, Tooltip("How long, in seconds, the Object will face a direction before it rotates towards its motion vector")]
        private float _flipDelay = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will start rotating to input")]
        private float _startTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will stop rotating to input")]
        private float _endTime = 1f;

        public void Construct(
            ObservePlayerMove observePlayerMove,
            Part.IMotor motor,
            Part.Orientation orientation,
            Animator animator
        ) {
            // The object's final facing unit vector angle
            var faceDirection = Vector3.zero;
            var flipTimer = 0f;

            ObserveStateEnter
                .Subscribe(_ => {
                    faceDirection = orientation.transform.forward;
                    flipTimer = 0f;
                })
                .AddTo(Streams);

            ObserveStateUpdate
                .Select(stateInfo => stateInfo.normalizedTime % 1)
                .Where(time => time >= _startTime && time <= _endTime)
                .WithLatestFrom(observePlayerMove(), (_, input) => input)
                .Subscribe(direction => {
                    if (direction != Vector3.zero) {
                        // If the movement direction is different enough from the facing direction,
                        // remain facing in the current direction for a short time. Allows the player to
                        // more easily execute Pull Attacks
                        var faceMotionDot = Vector3.Dot(direction, faceDirection);
                        if (faceMotionDot <= -0.01f && flipTimer < _flipDelay) {
                            flipTimer += Time.fixedDeltaTime;
                        } else {
                            faceDirection = motor.Project(direction.normalized);
                            flipTimer = 0f;
                        }
                    }

                    if (faceDirection != Vector3.zero) {
                        orientation.transform.rotation = Quaternion.RotateTowards(
                            orientation.transform.rotation,
                            Quaternion.LookRotation(faceDirection.normalized),
                            _rotationSpeed * Time.fixedDeltaTime
                        );
                    }
                })
                .AddTo(Streams);

            if (_snapOnExit) {
                ObserveStateExit
                    .Subscribe(_ => {
                        // Snap to the facing direction on state exit.
                        // Helps face the character in the intended direction when jumping mid-turn.
                        orientation.transform.rotation = Quaternion.LookRotation(faceDirection.normalized);
                    })
                    .AddTo(Streams);
            }
        }
    }
}