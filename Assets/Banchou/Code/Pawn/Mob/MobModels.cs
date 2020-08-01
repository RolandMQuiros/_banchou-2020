using System.Collections.Generic;

using MessagePack;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Mob {
    public enum ApproachStage : byte {
        Complete,
        Target,
        Position,
        Interrupted
    }

    [MessagePackObject]
    public class MobState {
        [Key(0)] public ApproachStage Stage = ApproachStage.Complete;
        [Key(1)] public Vector3 ApproachPosition = Vector3.zero;
        [Key(2)] public PawnId Target = PawnId.Empty;
        [Key(3)] public float StoppingDistance = 1f;

        public MobState() { }
        public MobState(in MobState prev) {
            Stage = prev.Stage;
            ApproachPosition = prev.ApproachPosition;
            Target = prev.Target;
            StoppingDistance = prev.StoppingDistance;
        }
    }

    [MessagePackObject]
    public class MobsState : Dictionary<PawnId, MobState> {
        public MobsState() { }
        public MobsState(in MobsState prev) : base(prev) { }
        public MobsState(in Dictionary<PawnId, MobState> dict) : base(dict) { }
    }
}