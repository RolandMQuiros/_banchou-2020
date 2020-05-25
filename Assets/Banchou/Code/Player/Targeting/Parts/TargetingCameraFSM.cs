using System;
using System.Linq;
using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Player.Targeting {
    [RequireComponent(typeof(Animator))]
    public class TargetingCameraFSM : MonoBehaviour {
        [SerializeField] private string _areEnemiesInRangeParameter = "AreEnemiesInRange";
        [SerializeField] private string _isLockedOnParameter = "IsLockedOn";
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState
        ) {
            var animator = GetComponent<Animator>();
            var isLockedOnHash = Animator.StringToHash(_isLockedOnParameter);
            var AreEnemiesInRangeHash = Animator.StringToHash(_areEnemiesInRangeParameter);

            observeState
                .Select(state => state.GetPlayerLockOnTarget(playerId) != PawnId.Empty)
                .DistinctUntilChanged()
                .Subscribe(isLockedOn => animator.SetBool(isLockedOnHash, isLockedOn))
                .AddTo(this);

            observeState
                .Select(state => state.GetPlayerTargets(playerId).Any())
                .DistinctUntilChanged()
                .Subscribe(inRange => animator.SetBool(AreEnemiesInRangeHash, inRange))
                .AddTo(this);
        }
    }
}