using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Mob {
    public enum ApproachStage {
        Complete,
        Target,
        Position,
        Interrupted
    }

    public class MobState {
        public ApproachStage Stage = ApproachStage.Complete;
        public Vector3 ApproachPosition = Vector3.zero;
        public PawnId Target = PawnId.Empty;
        public float StoppingDistance = 1f;

        public MobState() { }
        public MobState(in MobState prev) {
            Stage = prev.Stage;
            ApproachPosition = prev.ApproachPosition;
            Target = prev.Target;
            StoppingDistance = prev.StoppingDistance;
        }
    }

    public class MobsState : Dictionary<PawnId, MobState> {
        public MobsState() { }
        public MobsState(in MobsState prev) : base(prev) { }
        public MobsState(in Dictionary<PawnId, MobState> dict) : base(dict) { }
    }
}