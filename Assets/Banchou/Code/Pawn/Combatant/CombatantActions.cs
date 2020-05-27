﻿using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Combatant {
    namespace StateAction {
        public class Add : Pawn.StateAction.Add {
            public int Health;
        }

        public class CombatantAction {
            public PawnId CombatantId;
        }

        public class LockOn : CombatantAction {
            public PawnId To;
        }

        public class LockOff : CombatantAction { }

        public class PushCommand : CombatantAction {
            public Command Command;
            public float When;
        }
    }

    public class CombatantActions {
        public StateAction.Add Add(PawnId pawnId) {
            return new StateAction.Add {
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

        public StateAction.PushCommand PushCommand(PawnId combatantId, Command command) {
            return new StateAction.PushCommand {
                CombatantId = combatantId,
                Command = command,
                When = Time.fixedUnscaledTime
            };
        }
    }
}