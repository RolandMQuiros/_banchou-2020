using System;
using System.Linq;

using Redux;
using UniRx;

using Banchou.Combatant;
using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class LockOnToClosestTarget : FSMBehaviour {
        public void Construct(
            PawnId pawnId,
            IPawnInstance pawn,
            IObservable<GameState> observeState,
            ObservePlayerCommand observeCommands,
            IPawnInstances pawnInstances,
            Dispatcher dispatch,
            CombatantActions combatantActions
        ) {
            observeCommands()
                .Where(command => command == InputCommand.LockOn)
                .WithLatestFrom(
                    observeState.Select(state => state.GetCombatantTargets(pawnId)),
                    (_, targets) => targets
                )
                .Subscribe(targets => {
                    var target = pawnInstances.GetMany(targets)
                        .OrderBy(instance => (instance.Position - pawn.Position).sqrMagnitude)
                        .FirstOrDefault();
                    if (target != null) {
                        dispatch(combatantActions.LockOn(pawnId, target.PawnId));
                    }
                })
                .AddTo(this);

            observeCommands()
                .Where(command => command == InputCommand.LockOff)
                .WithLatestFrom(
                    observeState.Select(state => state.GetCombatantLockOnTarget(pawnId)),
                    (_, target) => target
                )
                .Where(target => target != PawnId.Empty)
                .Subscribe(target => {
                    dispatch(combatantActions.LockOff(pawnId));
                })
                .AddTo(this);
        }
    }
}