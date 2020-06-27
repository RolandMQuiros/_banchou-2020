using System;
using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Combatant.FSM {
    public class RigidbodyHitPush : FSMBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Rigidbody body
        ) {
            observeState
                .Select(state => state.GetCombatantLastHit(pawnId))
                .DistinctUntilChanged()
                .Where(_ => IsStateActive)
                .Subscribe(hit => {
                    body.AddForce(hit.Push, ForceMode.Force);
                })
                .AddTo(Streams);
        }
    }
}