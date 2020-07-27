using System.Net;
using System.Collections.Generic;

using UnityEngine;
using Newtonsoft.Json;

using Banchou.Pawn;
using Banchou.Utility;

namespace Banchou.Player {
    [JsonConverter(typeof(PlayerIdConverter))]
    public struct PlayerId {
        public static readonly PlayerId Empty = new PlayerId();
        private static int _idCounter = 1;
        public int Id { get; private set; }

        public static PlayerId Create() {
            return new PlayerId {
                Id = _idCounter++
            };
        }

        public PlayerId(int id) {
            Id = id;
        }

        #region Equality boilerplate
        public override bool Equals(object obj) {
            return GetType() == obj.GetType() && Id == ((PlayerId)obj).Id;
        }

        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PlayerId first, PlayerId second) => first.Equals(second);
        public static bool operator!=(PlayerId first, PlayerId second) => !first.Equals(second);
        #endregion
    }

    public enum InputSource {
        Local,
        Network,
        AI
    }

    public class NetworkInfo {
        public IPEndPoint IP;
        public int PeerId;
    }

    public class PlayerState {
        public InputSource Source = InputSource.Local;
        public string Name = null;
        public NetworkInfo NetworkInfo = null;

        public PawnId Pawn = PawnId.Empty;
        public HashSet<PawnId> Targets = new HashSet<PawnId>();

        public PlayerState() { }
        public PlayerState(in PlayerState prev) {
            Source = prev.Source;
            Name = prev.Name;
            NetworkInfo = prev.NetworkInfo;
            Pawn = prev.Pawn;
            Targets = prev.Targets;
        }
    }

    public class PlayersState : Dictionary<PlayerId, PlayerState> {
        public PlayersState() { }
        public PlayersState(in PlayersState prev) : base(prev) { }
        public PlayersState(in Dictionary<PlayerId, PlayerState> dict) : base(dict) { }
    }
}