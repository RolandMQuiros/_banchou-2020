﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Combatant {
    public enum Team : byte {
        None,
        Wobs,
        Shufflemen
    }

    public enum HitMedium : byte {
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

        public IEnumerable<PawnId> Targets = Enumerable.Empty<PawnId>();
        public PawnId LockOnTarget = PawnId.Empty;
        public Hit HitTaken = null;
        public Hit HitDealt = null;

        public CombatantState() { }
        public CombatantState(in CombatantState prev) {
            Health = prev.Health;
            Targets = prev.Targets;
            LockOnTarget = prev.LockOnTarget;
            HitTaken = prev.HitTaken;
            HitDealt = prev.HitDealt;
        }
    }

    public class CombatantsState {
        public Dictionary<PawnId, CombatantState> States = new Dictionary<PawnId, CombatantState>();
        public float LastUpdated = 0f;
        public CombatantsState() { }
        public CombatantsState(in CombatantsState prev) {
            States = prev.States;
            LastUpdated = prev.LastUpdated;
        }
    }
}