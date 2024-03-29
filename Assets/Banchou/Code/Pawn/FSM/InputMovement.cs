﻿using System;
using UnityEngine;
using UniRx;

using Banchou.Network;
using Banchou.Player;

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
            ObservePlayerMove observePlayerMove,
            Part.IMotor motor,
            Part.Orientation orientation,
            Animator animator,
            GetDeltaTime getDeltaTime
        ) {
            var speedOut = Animator.StringToHash(_movementSpeedOut);
            var rightSpeedOut = Animator.StringToHash(_velocityRightOut);
            var forwardSpeedOut = Animator.StringToHash(_velocityForwardOut);

            var speed = 0f;
            var forwardSpeed = 0f;
            var rightSpeed = 0f;

            ObserveStateUpdate
                .WithLatestFrom(observePlayerMove(), (_, direction) => direction)
                .CatchIgnoreLog()
                .Subscribe(
                    direction => {
                        var velocity = _movementSpeed * direction;
                        var offset = velocity * getDeltaTime();
                        motor.Move(offset);

                        // Write to output variables
                        if (!string.IsNullOrWhiteSpace(_movementSpeedOut)) {
                            if (speedOut != 0) {
                                speed = Mathf.MoveTowards(speed, velocity.magnitude, _acceleration);
                                animator.SetFloat(speedOut, speed);
                            }

                            if (rightSpeedOut != 0) {
                                rightSpeed = Mathf.MoveTowards(rightSpeed, Vector3.Dot(velocity, orientation.transform.right), _acceleration);
                                animator.SetFloat(rightSpeedOut, rightSpeed);
                            }

                            if (forwardSpeedOut != 0) {
                                forwardSpeed = Mathf.MoveTowards(forwardSpeed, Vector3.Dot(velocity, orientation.transform.forward), _acceleration);
                                animator.SetFloat(forwardSpeedOut, forwardSpeed);
                            }
                        }
                    }
                )
                .AddTo(this);
        }
    }
}