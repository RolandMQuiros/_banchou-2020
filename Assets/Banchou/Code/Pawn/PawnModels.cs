using System.Collections.Generic;

using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

using Banchou.Player;
using Banchou.Utility;

namespace Banchou.Pawn {
    [JsonConverter(typeof(PawnIdConverter)), MessagePackObject]
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
        public override bool Equals(object obj) {
            return GetType() == obj.GetType() && Id.Equals(((PawnId)obj).Id);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PawnId first, PawnId second) => first.Equals(second);
        public static bool operator!=(PawnId first, PawnId second) => !first.Equals(second);
        #endregion
    }

    [MessagePackObject]
    public class PawnFSMState {
        [Key(0)] public PawnId PawnId;
        [Key(1)] public int StateHash;
        [Key(2)] public bool IsLoop;
        [Key(3)] public float ClipLength;
        [Key(4)] public float FixedTimeAtChange;
        public PawnFSMState() { }
        public PawnFSMState(in PawnFSMState prev) {
            PawnId = prev.PawnId;
            StateHash = prev.StateHash;
            IsLoop = prev.IsLoop;
            ClipLength = prev.ClipLength;
            FixedTimeAtChange = prev.FixedTimeAtChange;
        }
    }

    [MessagePackObject]
    public class PawnSyncState {
        [Key(0)] public PawnId PawnId;
        [Key(1)] public Vector3 Position;
        [Key(2)] public Quaternion Rotation;
        public PawnSyncState() { }
        public PawnSyncState(in PawnSyncState prev) {
            PawnId = prev.PawnId;
            Position = prev.Position;
            Rotation = prev.Rotation;
        }
    }

    public enum PawnRollbackState : byte {
        Complete,
        RollingBack,
        FastForward
    }

    [MessagePackObject]
    public class PawnState {
        [Key(0)] public PlayerId PlayerId;
        [Key(1)] public string PrefabKey = string.Empty;
        [Key(2)] public float TimeScale = 1f;

        [Key(3)] public Vector3 SpawnPosition = Vector3.zero;
        [Key(4)] public Quaternion SpawnRotation = Quaternion.identity;

        [Key(5)] public PawnRollbackState RollbackState = PawnRollbackState.Complete;
        [Key(6)] public float RollbackCorrectionTime = 0f;
        [Key(7)] public PawnFSMState FSMState = new PawnFSMState();

        public PawnState() { }
        public PawnState(in PawnState prev) {
            PlayerId = prev.PlayerId;
            PrefabKey = prev.PrefabKey;
            TimeScale = prev.TimeScale;
            SpawnPosition = prev.SpawnPosition;
            SpawnRotation = prev.SpawnRotation;
            RollbackState = prev.RollbackState;
            RollbackCorrectionTime = prev.RollbackCorrectionTime;
            FSMState = prev.FSMState;
        }
    }

    [MessagePackObject]
    public class PawnsState {
        [Key(0)] public Dictionary<PawnId, PawnState> States = new Dictionary<PawnId, PawnState>();
        [Key(1)] public PawnSyncState LatestSync = new PawnSyncState();
        [Key(2)] public PawnFSMState LatestFSMChange = new PawnFSMState();

        public PawnsState() { }
        public PawnsState(in PawnsState prev) {
            States = prev.States;
            LatestSync = prev.LatestSync;
            LatestFSMChange = prev.LatestFSMChange;
        }
    }

}