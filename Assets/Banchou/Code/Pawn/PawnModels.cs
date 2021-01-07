using System.Collections.Generic;
using System.ComponentModel;

using MessagePack;
using UnityEngine;

using Banchou.Player;
using Banchou.Utility;

namespace Banchou.Pawn {
    [TypeConverter(typeof(PawnIdTypeConverter)), MessagePackObject]
    public struct PawnId {
        public static readonly PawnId Empty = new PawnId();
        private static int _idCounter = 1;
        [Key(0)] public int Id { get; private set; }

        public static PawnId Create() {
            return new PawnId {
                Id = _idCounter++
            };
        }

        public PawnId(int id) {
            Id = id;
        }

        #region Equality boilerplate
        public override bool Equals(object obj) => GetType() == obj.GetType() && Id.Equals(((PawnId)obj).Id);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PawnId first, PawnId second) => first.Equals(second);
        public static bool operator!=(PawnId first, PawnId second) => !first.Equals(second);
        #endregion
    }

    [MessagePackObject]
    public struct PawnFrameData {
        [Key(0)] public PawnId PawnId;
        [Key(1)] public List<int> StateHashes;
        [Key(2)] public List<float> NormalizedTimes;
        [Key(3)] public Dictionary<int, float> Floats;
        [Key(4)] public Dictionary<int, bool> Bools;
        [Key(5)] public Dictionary<int, int> Ints;
        [Key(6)] public Vector3 Position;
        [Key(7)] public Vector3 Forward;
        [Key(8)] public float When;

        public PawnFrameData(in PawnFrameData prev) {
            PawnId = prev.PawnId;
            StateHashes = prev.StateHashes;
            NormalizedTimes = prev.NormalizedTimes;
            Floats = prev.Floats;
            Bools = prev.Bools;
            Ints = prev.Ints;
            Position = prev.Position;
            Forward = prev.Forward;
            When = prev.When;
        }
    }

    public class PawnState {
        public PlayerId PlayerId;
        public string PrefabKey = string.Empty;
        public float TimeScale = 1f;

        public Vector3 SpawnPosition = Vector3.zero;
        public Quaternion SpawnRotation = Quaternion.identity;

        public PawnState() { }
        public PawnState(in PawnState prev) {
            PlayerId = prev.PlayerId;
            PrefabKey = prev.PrefabKey;
            TimeScale = prev.TimeScale;
            SpawnPosition = prev.SpawnPosition;
            SpawnRotation = prev.SpawnRotation;
        }
    }

    public class PawnsState {
        public Dictionary<PawnId, PawnState> States = new Dictionary<PawnId, PawnState>();
        public PawnFrameData? LatestPawnSyncFrame = null;
        public float LastUpdated = 0f;

        public PawnsState() { }
        public PawnsState(in PawnsState prev) {
            States = prev.States;
            LastUpdated = prev.LastUpdated;
            LatestPawnSyncFrame = prev.LatestPawnSyncFrame;
        }
    }
}