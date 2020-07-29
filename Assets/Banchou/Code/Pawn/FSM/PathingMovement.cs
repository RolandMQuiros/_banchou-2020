using System;

using UnityEngine;
using UnityEngine.AI;
using UniRx;
using Redux;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Mob.FSM {
    public class PathingMovement : FSMBehaviour {
        [Header("Movement")]
        [SerializeField, Tooltip("How quickly, in units per second, the object moves along its motion vector")]
        private float _movementSpeed = 8f;


        [Header("Animation Parameters")]
        [SerializeField, Tooltip("Animation parameter to write movement speed")]
        private string _movementSpeedOut = string.Empty;
        [SerializeField] private string _velocityRightOut = string.Empty;
        [SerializeField] private string _velocityForwardOut = string.Empty;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            MobActions pawnActions,
            NavMeshAgent agent,
            IMotor motor,
            Rigidbody body,
            Animator animator,
            Orientation orientation,
            IPawnInstances pawnInstances
        ) {
            observeState
                .Select(state => state.GetMob(pawnId))
                .Where(mob => mob != null)
                .DistinctUntilChanged()
                .Select(mob => {
                    switch (mob.Stage) {
                        case ApproachStage.Position:
                            return mob.ApproachPosition;
                        case ApproachStage.Target:
                            return pawnInstances.Get(mob.Target)?.Position;
                    }
                    return null;
                })
                .Where(targetPosition => targetPosition != null)
                .CatchIgnoreLog()
                .Subscribe(targetPosition => {
                    agent.nextPosition = body.position;
                    agent.speed = _movementSpeed;
                    agent.SetDestination(targetPosition.Value);
                })
                .AddTo(Streams);

            ObserveStateUpdate
                .WithLatestFrom(observeState, (_, state) => state.IsMobApproaching(pawnId))
                .Where(isApproaching => isApproaching && agent.isPathStale)
                .DistinctUntilChanged()
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    dispatch(pawnActions.ApproachInterrupted(pawnId));
                })
                .AddTo(Streams);

            ObserveStateUpdate
                .WithLatestFrom(observeState, (_, state) => state.IsMobApproaching(pawnId))
                .Where(isApproaching => isApproaching && !(agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
                .Subscribe(_ => {
                    dispatch(pawnActions.ApproachCompleted(pawnId));
                })
                .AddTo(Streams);


            var speedOut = Animator.StringToHash(_movementSpeedOut);
            var rightSpeedOut = Animator.StringToHash(_velocityRightOut);
            var forwardSpeedOut = Animator.StringToHash(_velocityForwardOut);

            Vector3 velocity = Vector3.zero;
            ObserveStateUpdate
                .WithLatestFrom(observeState, (_, state) => state.IsMobApproaching(pawnId))
                .Where(isApproaching => isApproaching)
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    motor.Move(agent.desiredVelocity * Time.fixedDeltaTime);

                    // Write to output variables
                    if (!string.IsNullOrWhiteSpace(_movementSpeedOut)) {
                        animator.SetFloat(speedOut, agent.velocity.magnitude);
                        animator.SetFloat(rightSpeedOut, Vector3.Dot(agent.desiredVelocity, orientation.transform.right));
                        animator.SetFloat(forwardSpeedOut, Vector3.Dot(agent.desiredVelocity, orientation.transform.forward));
                    }
                })
                .AddTo(Streams);

            // ObserveStateUpdate
            //     .Sample(TimeSpan.FromSeconds(1f / 15f))
            //     .WithLatestFrom(observeState, (_, state) => state.IsMobApproaching(pawnId))
            //     .Where(isApproaching => isApproaching)
            //     .Subscribe(_ => {
            //         // Write to output variables
            //         if (!string.IsNullOrWhiteSpace(_movementSpeedOut)) {
            //             animator.SetFloat(speedOut, velocity.magnitude);
            //             animator.SetFloat(rightSpeedOut, Vector3.Dot(velocity, _movementSpeed * orientation.transform.right));
            //             animator.SetFloat(forwardSpeedOut, Vector3.Dot(velocity, _movementSpeed * orientation.transform.forward));
            //         }
            //     })
            //     .AddTo(Streams);
        }
    }
}
