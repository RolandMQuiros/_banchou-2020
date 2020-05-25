using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Player {
    public struct PlayerId {
        public static readonly PlayerId Empty = new PlayerId();
        public Guid Id;

        public static PlayerId Create() {
            return new PlayerId {
                Id = Guid.NewGuid()
            };
        }

        #region Equality boilerplate
        public override bool Equals(object obj) {
            return GetType() == obj.GetType() && Id == ((PlayerId)obj).Id;
        }

        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public static bool operator==(PlayerId first, PlayerId second) => first.Equals(second);
        public static bool operator!=(PlayerId first, PlayerId second) => !first.Equals(second);
        #endregion
    }

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

        LockOn,
        LockOff
    }

    public enum InputSource {
        LocalSingle,
        LocalMulti,
        Network,
        AI
    }

    public struct PushedCommand : IEquatable<PushedCommand> {
        public static readonly PushedCommand Empty = new PushedCommand();
        public Command Command;
        public float When;

        public bool Equals(PushedCommand other) => other.Command == Command && other.When == When;
    }

    public class PlayerState {
        public InputSource Source = InputSource.LocalSingle;
        public Team Team = Team.None;

        public List<PawnId> Pawns = new List<PawnId>();
        public List<PawnId> SelectedPawns = new List<PawnId>();

        public HashSet<PawnId> Targets = new HashSet<PawnId>();

        public IEnumerable<PawnId> LockOnHistory = Enumerable.Empty<PawnId>();
        public IEnumerable<PawnId> LockOnFuture = Enumerable.Empty<PawnId>();

        public PawnId LockOnTarget = PawnId.Empty;

        public Vector2 InputMovement;
        public Vector2 InputLook;
        public bool InputLockOn;

        public PushedCommand LastCommand;

        public PlayerState() { }
        public PlayerState(in PlayerState prev) {
            Source = prev.Source;
            Team = prev.Team;
            SelectedPawns = prev.SelectedPawns;
            Pawns = prev.Pawns;
            SelectedPawns = prev.SelectedPawns;
            Targets = prev.Targets;

            LockOnHistory = prev.LockOnHistory;
            LockOnFuture = prev.LockOnFuture;
            LockOnTarget = prev.LockOnTarget;

            InputMovement = prev.InputMovement;
            InputLook = prev.InputLook;

            LastCommand = prev.LastCommand;
        }
    }

    public class PlayersState : Dictionary<PlayerId, PlayerState> {
        public PlayersState() { }
        public PlayersState(in PlayersState prev) : base(prev) { }
        public PlayersState(in Dictionary<PlayerId, PlayerState> dict) : base(dict) { }
    }
}