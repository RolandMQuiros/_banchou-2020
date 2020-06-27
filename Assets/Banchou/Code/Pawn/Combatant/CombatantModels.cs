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

    public enum Command {
        None,
        LightAttack,
        HeavyAttack
    }

    public struct PushedCommand : IEquatable<PushedCommand> {
        public static readonly PushedCommand Empty = new PushedCommand();
        public Command Command;
        public float When;

        public bool Equals(PushedCommand other) => other.Command == Command && other.When == When;
    }

    public struct Hit : IEquatable<Hit> {
        public static readonly Hit Empty = new Hit();
        public PawnId By;
        public Vector3 Push;
        public int Strength;
        public float When;

        public bool Equals(Hit other) {
            return By == other.By &&
                Push == other.Push &&
                Strength == other.Strength &&
                When == other.When;
        }
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
        public PushedCommand LastCommand = PushedCommand.Empty;
        public Hit LastHit = Hit.Empty;

        public CombatantState() { }
        public CombatantState(in CombatantState prev) {
            Health = prev.Health;
            LockOnTarget = prev.LockOnTarget;
            LastCommand = prev.LastCommand;
            LastHit = prev.LastHit;
        }
    }

    public class CombatantsState : Dictionary<PawnId, CombatantState> {
        public CombatantsState() { }
        public CombatantsState(in CombatantsState prev) : base(prev) { }
    }
}