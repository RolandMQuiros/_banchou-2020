using System;
using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Combatant {
    public class CombatantFSM : MonoBehaviour {
        [Header("State Machine Parameters")]
        [SerializeField] private string _healthParameterName = "Health";
        [SerializeField] private string _isLockedOnParameterName = "IsLockedOn";

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Animator animator
        ) {
            var healthHash = Animator.StringToHash(_healthParameterName);
            var isLockedOnHash = Animator.StringToHash(_isLockedOnParameterName);

            observeState
                .Select(state => state.GetCombatantHealth(pawnId))
                .Subscribe(health => animator.SetFloat(healthHash, health))
                .AddTo(this);

            observeState
                .Select(state => state.GetCombatantTarget(pawnId))
                .DistinctUntilChanged()
                .Select(target => target != PawnId.Empty)
                .Subscribe(isLockedOn => animator.SetBool(isLockedOnHash, isLockedOn))
                .AddTo(this);
        }

        private void OnAnimatorMove() { }
    }
}