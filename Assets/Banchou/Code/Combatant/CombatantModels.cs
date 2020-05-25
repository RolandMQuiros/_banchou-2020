using System;
using System.Collections.Generic;

using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Combatant {
    public enum Team {
        None,
        Wobs,
        Shufflemen
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

    public enum Command {
        None,
        LightAttack,
        HeavyAttack,
        Jump
    }

    public struct PushedCommand : IEquatable<PushedCommand> {
        public static readonly PushedCommand Empty = new PushedCommand();
        public Command Command;
        public float When;

        public bool Equals(PushedCommand other) => other.Command == Command && other.When == When;
    }

    public class CombatantState {
        public Team Team = Team.None;
        public int Health = 0;
        public PlayerId Player = PlayerId.Empty;
        public PawnId LockOnTarget = PawnId.Empty;
        public PushedCommand LastCommand = PushedCommand.Empty;

        public CombatantState() { }
        public CombatantState(in CombatantState prev) {
            Team = prev.Team;
            Health = prev.Health;
            Player = prev.Player;
            LockOnTarget = prev.LockOnTarget;
            LastCommand = prev.LastCommand;
        }
    }

    public class CombatantsState : Dictionary<PawnId, CombatantState> {
        public CombatantsState() { }
        public CombatantsState(in CombatantsState prev) : base(prev) { }
    }
}