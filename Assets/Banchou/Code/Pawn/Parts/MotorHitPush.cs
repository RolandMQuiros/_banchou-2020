using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    public class MotorHitPush : MonoBehaviour {
        [SerializeField] private float _deceleration = 100f;
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            IMotor motor,
            GetDeltaTime getDeltaTime
        ) {
            var pushVelocity = Vector3.zero;

            observeState
                .Select(state => state.GetCombatantHitTaken(pawnId))
                .DistinctUntilChanged()
                .Where(hit => hit != null)
                .Where(_ => isActiveAndEnabled)
                .CatchIgnore((Exception error) => Debug.LogException(error))
                .Subscribe(hit => { pushVelocity = hit.Push; })
                .AddTo(this);

            this.FixedUpdateAsObservable()
                .Where(_ => pushVelocity != Vector3.zero)
                .Subscribe(_ => {
                    pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, _deceleration * getDeltaTime());
                    motor.Move(pushVelocity * getDeltaTime());
                })
                .AddTo(this);
        }

        private void Start() { }
    }
}