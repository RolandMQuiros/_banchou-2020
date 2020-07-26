using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using UnityEngine;

using Banchou.Player;
using Banchou.Utility;

namespace Banchou.Pawn {
    [JsonConverter(typeof(PawnIdConverter))]
    public struct PawnId {
        public static readonly PawnId Empty = new PawnId();
        private static int _idCounter = 0;
        public int Id;

        public static PawnId Create() {
            return new PawnId {
                Id = _idCounter++
            };
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

    public enum PawnRollbackState {
        Complete,
        RollingBack,
        FastForward
    }

    public class PawnState {
        public PlayerId PlayerId;
        public string PrefabKey = string.Empty;
        public float TimeScale = 1f;

        public PawnRollbackState RollbackState = PawnRollbackState.Complete;
        public float RollbackCorrectionTime = 0f;
        public PawnFSMState FSMState = new PawnFSMState();

        public PawnState() { }
        public PawnState(in PawnState prev) {
            PlayerId = prev.PlayerId;
            PrefabKey = prev.PrefabKey;
            TimeScale = prev.TimeScale;
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