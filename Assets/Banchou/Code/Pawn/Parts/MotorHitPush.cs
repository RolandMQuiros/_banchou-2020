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
            IMotor motor
        ) {
            var pushVelocity = Vector3.zero;

            observeState
                .Select(state => state.GetCombatantLastHit(pawnId))
                .DistinctUntilChanged()
                .CatchIgnore((Exception error) => Debug.LogException(error))
                .Subscribe(hit => {
                    pushVelocity = hit.Push;
                })
                .AddTo(this);

            this.FixedUpdateAsObservable()
                .Where(_ => pushVelocity != Vector3.zero)
                .Subscribe(_ => {
                    pushVelocity = Vector3.MoveTowards(pushVelocity, Vector3.zero, _deceleration * Time.fixedDeltaTime);
                    motor.Move(pushVelocity * Time.fixedDeltaTime);
                })
                .AddTo(this);
        }
    }
}