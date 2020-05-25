using System;
using System.Linq;
using UnityEngine;
using UniRx;

using Banchou.Pawn.Part;

namespace Banchou.Pawn.FSM {
    public class RotateToClosestTarget : FSMBehaviour {
        [SerializeField] private float _targetingPrecision = 0.4f;
        [SerializeField] private float _rotationSpeed = 1000f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will start rotating to input")]
        private float _startTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will stop rotating to input")]
        private float _endTime = 1f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Rigidbody body,
            Orientation orientation,
            IPawnInstances pawnInstances
        ) {
            var observeTarget = observeState
                .Select(state => state.GetPawnPlayer(pawnId)?.Targets)
                .DistinctUntilChanged()
                .Select(targets => targets ?? Enumerable.Empty<PawnId>());

            var chooseTargetOnEnter = ObserveStateEnter
                .WithLatestFrom(observeTarget, (_, targets) => targets)
                .Select(targets => pawnInstances.GetMany(targets)
                    .Where(instance => {
                        if (instance != null) {
                            var projected = Vector3.Dot(
                                (instance.Position - body.transform.position).normalized,
                                orientation.transform.forward
                            );
                            return projected > _targetingPrecision;
                        }
                        return false;
                    })
                    .OrderBy(t => (t.Position - body.transform.position).sqrMagnitude)
                    .FirstOrDefault()
                );

            ObserveStateUpdate
                .Select(stateInfo => stateInfo.normalizedTime % 1)
                .Where(time => time >= _startTime && time <= _endTime)
                .WithLatestFrom(chooseTargetOnEnter, (_, target) => target)
                .Where(target => target != null)
                .Subscribe(target => {
                    var direction = Vector3.ProjectOnPlane(
                        target.Position - body.transform.position,
                        body.transform.up
                    ).normalized;
                    orientation.transform.rotation = Quaternion.RotateTowards(
                        orientation.transform.rotation,
                        Quaternion.LookRotation(direction),
                        _rotationSpeed * Time.fixedDeltaTime
                    );
                })
                .AddTo(Streams);
        }
    }
}