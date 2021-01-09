using UnityEngine;
using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Combatant {
    namespace StateAction {
        public struct AddCombatant {
            public PawnId PawnId;
            public int Health;
            public float When;
        }

        public interface ICombatantAction {
            PawnId CombatantId { get; }
            float When { get; }
        }

        public struct AddTarget : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId Target;
            public float When { get; set; }
        }

        public struct RemoveTarget : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId Target;
            public float When { get; set; }
        }

        public struct LockOn : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public PawnId To;
            public float When { get; set; }
        }

        public struct LockOff : ICombatantAction {
            public PawnId CombatantId { get; set; }
            public float When { get; set; }
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
        private GetTime _getTime;

        public CombatantActions(GetTime getTime) {
            _getTime = getTime;
        }

        public StateAction.AddCombatant Add(PawnId pawnId, float? when = null) => new StateAction.AddCombatant {
            PawnId = pawnId,
            Health = 100,
            When = when ?? _getTime()
        };

        public StateAction.AddTarget AddTarget(PawnId combatantId, PawnId target, float? when = null) => new StateAction.AddTarget {
            CombatantId = combatantId,
            Target = target,
            When = when ?? _getTime()
        };

        public StateAction.RemoveTarget RemoveTarget(PawnId combatantId, PawnId target, float? when = null) {
            return new StateAction.RemoveTarget {
                CombatantId = combatantId,
                Target = target,
                When = when ?? _getTime()
            };
        }

        public StateAction.LockOn LockOn(PawnId combatantId, PawnId to, float? when = null) {
            return new StateAction.LockOn {
                CombatantId = combatantId,
                To = to,
                When = when ?? _getTime()
            };
        }

        public StateAction.LockOff LockOff(PawnId combatantId, float? when = null) {
            return new StateAction.LockOff {
                CombatantId = combatantId,
                When = when ?? _getTime()
            };
        }

        public StateAction.Hit Hit(PawnId from, PawnId to, HitMedium medium, int strength, Vector3 push = default(Vector3), float? when = null) {
            return new StateAction.Hit {
                From = from,
                To = to,
                Medium = medium,
                Push = push,
                When = when ?? _getTime()
            };
        }
    }
}