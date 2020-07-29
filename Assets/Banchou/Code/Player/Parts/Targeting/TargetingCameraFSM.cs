using System;
using System.Linq;
using UnityEngine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(Animator))]
    public class TargetingCameraFSM : MonoBehaviour {
        [SerializeField] private string _areEnemiesInRangeParameter = "AreEnemiesInRange";
        [SerializeField] private string _isLockedOnParameter = "IsLockedOn";
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState
        ) {
            var animator = GetComponent<Animator>();
            var isLockedOnHash = Animator.StringToHash(_isLockedOnParameter);
            var AreEnemiesInRangeHash = Animator.StringToHash(_areEnemiesInRangeParameter);

            observeState
                .Select(state => state.GetCombatantTargets(pawnId).Any())
                .DistinctUntilChanged()
                .Subscribe(inRange => animator.SetBool(AreEnemiesInRangeHash, inRange))
                .AddTo(this);
        }
    }
}