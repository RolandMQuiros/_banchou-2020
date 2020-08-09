using System;
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

    public class PawnFSMState {
        public PawnId PawnId;
        public int StateHash;
        public bool IsLoop;
        public float ClipLength;
        public float FixedTimeAtChange;
        public PawnFSMState() { }
        public PawnFSMState(in PawnFSMState prev) {
            PawnId = prev.PawnId;
            StateHash = prev.StateHash;
            IsLoop = prev.IsLoop;
            ClipLength = prev.ClipLength;
            FixedTimeAtChange = prev.FixedTimeAtChange;
        }
    }

    public class PawnSyncState {
        public PawnId PawnId;
        public Vector3 Position;
        public Quaternion Rotation;
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

    public class PawnState {
        public PlayerId PlayerId;
        public string PrefabKey = string.Empty;
        public float TimeScale = 1f;

        public Vector3 SpawnPosition = Vector3.zero;
        public Quaternion SpawnRotation = Quaternion.identity;

        public PawnRollbackState RollbackState = PawnRollbackState.Complete;
        public float RollbackCorrectionTime = 0f;
        public PawnFSMState FSMState = new PawnFSMState();

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

    public class PawnsState {
        public Dictionary<PawnId, PawnState> States = new Dictionary<PawnId, PawnState>();
        public PawnSyncState LatestSync = new PawnSyncState();
        public PawnFSMState LatestFSMChange = new PawnFSMState();

        public PawnsState() { }
        public PawnsState(in PawnsState prev) {
            States = prev.States;
            LatestSync = prev.LatestSync;
            LatestFSMChange = prev.LatestFSMChange;
        }
    }

}