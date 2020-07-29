using System;
using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Combatant.FSM {
    public class RigidbodyHitPush : FSMBehaviour {
        [SerializeField] private ForceMode _forceMode;
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Rigidbody body
        ) {
            observeState
                .Select(state => state.GetCombatantHitTaken(pawnId))
                .DistinctUntilChanged()
                .Where(_ => IsStateActive)
                .Subscribe(hit => {
                    body.AddForce(hit.Push, ForceMode.Force);
                })
                .AddTo(this);
        }
    }
}