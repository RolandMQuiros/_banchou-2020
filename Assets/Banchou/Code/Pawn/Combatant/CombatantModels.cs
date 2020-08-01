using System.Collections.Generic;
using System.Linq;

using MessagePack;
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

    [MessagePackObject]
    public class Hit {
        [Key(0)] public HitMedium Medium;
        [Key(1)] public PawnId From;
        [Key(2)] public PawnId To;
        [Key(3)] public Vector3 Push;
        [Key(4)] public int Strength;
        [Key(5)] public float When;
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

    [MessagePackObject]
    public class CombatantState {
        [Key(0)] public int Health = 0;

        [Key(1)] public IEnumerable<PawnId> Targets = Enumerable.Empty<PawnId>();
        [Key(2)] public PawnId LockOnTarget = PawnId.Empty;
        [Key(3)] public Hit HitTaken = null;
        [Key(4)] public Hit HitDealt = null;

        public CombatantState() { }
        public CombatantState(in CombatantState prev) {
            Health = prev.Health;
            Targets = prev.Targets;
            LockOnTarget = prev.LockOnTarget;
            HitTaken = prev.HitTaken;
            HitDealt = prev.HitDealt;
        }
    }

    [MessagePackObject]
    public class CombatantsState : Dictionary<PawnId, CombatantState> {
        public CombatantsState() { }
        public CombatantsState(in CombatantsState prev) : base(prev) { }
    }
}