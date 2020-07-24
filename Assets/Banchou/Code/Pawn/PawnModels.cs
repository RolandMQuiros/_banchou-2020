using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using Banchou.Player;
using Banchou.Utility;

namespace Banchou.Pawn {
    [JsonConverter(typeof(PawnIdConverter))]
    public struct PawnId {
        public static readonly PawnId Empty = new PawnId();
        public Guid Id;

        public static PawnId Create() {
            return new PawnId {
                Id = Guid.NewGuid()
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

    public struct PawnFSMState {
        public int StateHash;
        public bool IsLoop;
        public float ClipLength;
        public float FixedTimeAtChange;
        public PawnFSMState(in PawnFSMState prev) {
            StateHash = prev.StateHash;
            IsLoop = prev.IsLoop;
            ClipLength = prev.ClipLength;
            FixedTimeAtChange = prev.FixedTimeAtChange;
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

    public class PawnsState : Dictionary<PawnId, PawnState> {
        public PawnsState() { }
        public PawnsState(in PawnsState prev) : base(prev) { }
        public PawnsState(in Dictionary<PawnId, PawnState> dict) : base(dict) { }
    }

}