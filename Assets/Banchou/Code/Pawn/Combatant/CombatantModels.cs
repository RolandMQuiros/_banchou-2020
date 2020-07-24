using System;
using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Combatant {
    public enum Team {
        None,
        Wobs,
        Shufflemen
    }

    public enum HitMedium {
        Environment,
        Melee,
        Ranged
    }

    public class Hit {
        public HitMedium Medium;
        public PawnId From;
        public PawnId To;
        public Vector3 Push;
        public int Strength;
        public float When;
    }

    public static class TeamExt {
        public static bool IsHostile(this Team team, Team other) {
            return team != Team.None && other != Team.None &&
                (
                    (team == Team.Wobs && other == Team.Shufflemen) ||
                    (team == Team.Shufflemen && other == Team.Wobs)
                );
        }
    }

    public class CombatantState {
        public int Health = 0;

        public PawnId LockOnTarget = PawnId.Empty;
        public Hit HitTaken = null;
        public Hit HitDealt = null;

        public CombatantState() { }
        public CombatantState(in CombatantState prev) {
            Health = prev.Health;
            LockOnTarget = prev.LockOnTarget;
            HitTaken = prev.HitTaken;
            HitDealt = prev.HitDealt;
        }
    }

    public class CombatantsState : Dictionary<PawnId, CombatantState> {
        public CombatantsState() { }
        public CombatantsState(in CombatantsState prev) : base(prev) { }
    }
}