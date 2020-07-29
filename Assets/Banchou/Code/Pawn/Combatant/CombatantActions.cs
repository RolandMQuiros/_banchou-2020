using UnityEngine;
using Redux;

using Banchou.Board;
using Banchou.Pawn;

namespace Banchou.Combatant {
    namespace StateAction {
        public struct AddCombatant {
            public PawnId PawnId;
            public int Health;
        }

        public interface ICombatantAction {
            PawnId CombatantId { get; }
        }

        public struct AddTarget : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId Target;
        }

        public struct RemoveTarget : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId Target;
        }

        public struct LockOn : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId To;
        }

        public struct LockOff : ICombatantAction {
            public PawnId CombatantId { get; set; }
        }

        public struct Hit {
            public PawnId From;
            public PawnId To;
            public HitMedium Medium;
            public int Strength;
            public Vector3 Push;
            public float When;
        }
    }

    public class CombatantActions {
        public StateAction.AddCombatant Add(PawnId pawnId) => new StateAction.AddCombatant {
            PawnId = pawnId,
            Health = 100
        };

        public StateAction.AddTarget AddTarget(PawnId combatantId, PawnId target) => new StateAction.AddTarget {
            CombatantId = combatantId,
            Target = target
        };

        public StateAction.RemoveTarget RemoveTarget(PawnId combatantId, PawnId target) => new StateAction.RemoveTarget {
            CombatantId = combatantId,
            Target = target
        };

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