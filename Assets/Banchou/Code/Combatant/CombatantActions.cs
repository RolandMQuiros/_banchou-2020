using UnityEngine;

using System.Linq;
using Redux;

using Banchou.Pawn;

namespace Banchou.Combatant {
    namespace StateAction {
        public class Add : Pawn.StateAction.Add {
            public int Health;
            public Team Team;
        }

        public class CombatantAction {
            public PawnId PawnId;
        }

        public class PushCommand {
            public Player.Command Command;
            public float When;
        }

        public class LockOn : CombatantAction {
            public PawnId To;
        }

        public class LockOff : CombatantAction { }
    }

    public class CombatantActions {
        private IPawnInstances _pawnInstances;

        public void Construct(IPawnInstances pawnInstances) {
            _pawnInstances = pawnInstances;
        }

        public StateAction.Add Add(PawnId pawnId) {
            return new StateAction.Add {
                PawnId = pawnId
            };
        }

        public StateAction.LockOn LockOn(PawnId pawnId, PawnId to) {
            return new StateAction.LockOn {
                PawnId = pawnId,
                To = to
            };
        }

        public StateAction.LockOff LockOff(PawnId pawnId) {
            return new StateAction.LockOff {
                PawnId = pawnId
            };
        }

        public ActionsCreator<GameState> LockOnToClosestInFront(PawnId pawnId) => (dispatch, getState) => {
            var combatant = getState().GetCombatant(pawnId);
            var instance = _pawnInstances.Get(pawnId);
            if (combatant != null && instance != null) {
                var target = getState().Combatants
                    .Where(pair => pair.Key != pawnId)
                    .Where(pair => pair.Value.Team.IsHostile(combatant.Team))
                    .Select(pair => _pawnInstances.Get(pair.Key))
                    .Where(enemy => Vector3.Dot(Vector3.Normalize(enemy.Position - instance.Position), instance.Forward) > 0f)
                    .OrderBy(enemy => (enemy.Position - instance.Position).sqrMagnitude)
                    .Select(enemy => enemy.PawnId)
                    .FirstOrDefault();

                dispatch(LockOn(pawnId, target));
            }
        };
    }
}