using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Combatant {
    namespace StateAction {
        public class AddCombatant : Pawn.StateAction.AddPawn {
            public int Health;
        }

        public class CombatantAction {
            public PawnId CombatantId;
        }

        public class LockOn : CombatantAction {
            public PawnId To;
        }

        public class LockOff : CombatantAction { }

        public class Hit {
            public PawnId From;
            public PawnId To;
            public HitMedium Medium;
            public int Strength;
            public Vector3 Push;
            public float When;
        }
    }

    public class CombatantActions {
        public StateAction.AddCombatant Add(PawnId pawnId) {
            return new StateAction.AddCombatant {
                PawnId = pawnId
            };
        }

        public StateAction.LockOn LockOn(PawnId combatantId, PawnId to) {
            return new StateAction.LockOn {
                CombatantId = combatantId,
                To = to
            };
        }

        public StateAction.LockOff LockOff(PawnId combatantId) {
            return new StateAction.LockOff {
                CombatantId = combatantId
            };
        }

        public StateAction.Hit Hit(PawnId from, PawnId to, HitMedium medium, int strength, Vector3 push = default(Vector3)) {
            return new StateAction.Hit {
                From = from,
                To = to,
                Medium = medium,
                Push = push,
                When = Time.unscaledTime
            };
        }
    }
}