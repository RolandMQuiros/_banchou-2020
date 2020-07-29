using Redux;
using UniRx;
using UniRx.Triggers;
using UnityEngine;


using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Combatant {
    [RequireComponent(typeof(Collider))]
    public class TargetingVolume : MonoBehaviour {
        public void Construct(
            PawnId pawnId,
            Dispatcher dispatch,
            CombatantActions combatantActions
        ) {
            this.OnTriggerEnterAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable?.PawnId != PawnId.Empty && pawnId != targetable.PawnId) {
                        dispatch(combatantActions.AddTarget(pawnId, targetable.PawnId));
                    }
                })
                .AddTo(this);

            this.OnTriggerExitAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable?.PawnId != PawnId.Empty && pawnId != targetable.PawnId) {
                        dispatch(combatantActions.RemoveTarget(pawnId, targetable.PawnId));
                    }
                })
                .AddTo(this);
        }
    }
}