using System;
using UnityEngine;
using UniRx;

using Banchou.Pawn.Part;
using Banchou.Combatant;

namespace Banchou.Pawn.FSM {
    public class RotateToLockOnTarget : FSMBehaviour {
        [SerializeField]private float _rotationSpeed = 1000f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will start rotating to input")]
        private float _startTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in normalized state time, the Object will stop rotating to input")]
        private float _endTime = 1f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Orientation orientation,
            IPawnInstances pawnInstance,
            GetDeltaTime getDeltaTime
        ) {
            var observeTarget = observeState
                .Select(state => state.GetCombatantLockOnTarget(pawnId))
                .DistinctUntilChanged();

            ObserveStateUpdate
                .Select(stateUnit => stateUnit.StateInfo.normalizedTime % 1)
                .Where(time => time >= _startTime && time <= _endTime)
                .WithLatestFrom(observeTarget, (_, target) => target)
                .Where(target => target != PawnId.Empty)
                .Subscribe(target => {
                    var targetInstance = pawnInstance.Get(target);
                    var direction = Vector3.ProjectOnPlane(
                        targetInstance.Position - orientation.transform.position,
                        orientation.transform.up
                    ).normalized;

                    orientation.TrackRotation(
                        Quaternion.RotateTowards(
                            orientation.transform.rotation,
                            Quaternion.LookRotation(direction),
                            _rotationSpeed * getDeltaTime()
                        )
                    );
                })
                .AddTo(this);
        }
    }
}