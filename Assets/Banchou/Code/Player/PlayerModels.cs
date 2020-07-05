using System;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;

using Banchou.Pawn;
using Banchou.Utility;

namespace Banchou.Player {
    [JsonConverter(typeof(PlayerIdConverter))]
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

    public enum InputSource {
        LocalSingle,
        LocalMulti,
        Network,
        AI
    }

    public class PlayerState {
        public InputSource Source = InputSource.LocalSingle;

        public PawnId Pawn = PawnId.Empty;
        public HashSet<PawnId> Targets = new HashSet<PawnId>();

        public Vector2 InputMovement;
        public Vector2 InputLook;

        public PlayerState() { }
        public PlayerState(in PlayerState prev) {
            Source = prev.Source;

            Pawn = prev.Pawn;
            Targets = prev.Targets;

            InputMovement = prev.InputMovement;
            InputLook = prev.InputLook;
        }
    }

    public class PlayersState : Dictionary<PlayerId, PlayerState> {
        public PlayersState() { }
        public PlayersState(in PlayersState prev) : base(prev) { }
        public PlayersState(in Dictionary<PlayerId, PlayerState> dict) : base(dict) { }
    }
}