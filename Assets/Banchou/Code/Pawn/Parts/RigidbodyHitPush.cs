using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    public class RigidbodyHitPush : MonoBehaviour {
        [SerializeField] private ForceMode _forceMode;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Rigidbody body
        ) {
            observeState
                .Select(state => state.GetCombatantLastHit(pawnId))
                .DistinctUntilChanged()
                .CatchIgnore((Exception error) => Debug.LogException(error))
                .Subscribe(hit => {
                    body.AddForce(hit.Push, _forceMode);
                })
                .AddTo(this);
        }
    }
}