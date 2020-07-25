using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    public class RigidbodyHitPush : MonoBehaviour {
        [SerializeField] private ForceMode _forceMode = ForceMode.VelocityChange;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Rigidbody body
        ) {
            observeState
                .Select(state => state.GetCombatantHitTaken(pawnId))
                .DistinctUntilChanged()
                .Where(hit => hit != null)
                .CatchIgnore((Exception error) => Debug.LogException(error))
                .Subscribe(hit => {
                    body.AddForce(hit.Push, _forceMode);
                })
                .AddTo(this);
        }
    }
}