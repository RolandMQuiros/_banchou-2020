using System.Net;
using System.Collections.Generic;
using System.ComponentModel;

using MessagePack;

using Banchou.Pawn;
using Banchou.Utility;

namespace Banchou.Player {
    [TypeConverter(typeof(PlayerIdTypeConverter)), MessagePackObject]
    public struct PlayerId {
        public static readonly PlayerId Empty = new PlayerId();
        private static int _idCounter = 1;
        [Key(0)] public int Id { get; private set; }

        public static PlayerId Create() {
            return new PlayerId {
                Id = _idCounter++
            };
        }

        public PlayerId(int id) {
            Id = id;
        }

        #region Equality boilerplate
        public override bool Equals(object obj) => GetType() == obj.GetType() && Id == ((PlayerId)obj).Id;
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PlayerId first, PlayerId second) => first.Equals(second);
        public static bool operator!=(PlayerId first, PlayerId second) => !first.Equals(second);
        #endregion
    }

    public class NetworkInfo {
        public IPEndPoint IP;
        public int PeerId;

        public NetworkInfo() { }
        public NetworkInfo(in NetworkInfo prev) {
            IP = prev.IP;
            PeerId = prev.PeerId;
        }
    }

    public class PlayerState {
        public string PrefabKey = string.Empty;
        public string Name = null;
        public NetworkInfo NetworkInfo = null;
        public PawnId Pawn = PawnId.Empty;

        public PlayerState() { }
        public PlayerState(in PlayerState prev) {
            PrefabKey = prev.PrefabKey;
            Name = prev.Name;
            NetworkInfo = prev.NetworkInfo;
            Pawn = prev.Pawn;
        }
    }

    public class PlayersState {
        public Dictionary<PlayerId, PlayerState> States = new Dictionary<PlayerId, PlayerState>();

        public PlayersState() { }
        public PlayersState(in PlayersState prev) {
            States = prev.States;
        }
    }
}