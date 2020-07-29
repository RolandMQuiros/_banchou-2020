using System;
using System.Linq;

using UniRx;
using UnityEngine;

using Banchou.Combatant;

namespace Banchou.Pawn.FSM {
    public class LockOnToClosestTarget : FSMBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState
        ) {
            ObserveStateEnter
                .WithLatestFrom(
                    observeState.Select(state => state.GetCombatantTargets(pawnId)),
                    (_, targets) => targets
                )
                .Subscribe(targets => {

                })
                .AddTo(this);
        }
    }
}