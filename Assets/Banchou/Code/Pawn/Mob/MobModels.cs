using System.Collections.Generic;
using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Mob {
    public enum ApproachStage : byte {
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

    public class MobsState {
        public Dictionary<PawnId, MobState> States = new Dictionary<PawnId, MobState>();
        public float LastUpdated = 0f;
        public MobsState() { }
        public MobsState(in MobsState prev) {
            States = prev.States;
            LastUpdated = prev.LastUpdated;
        }
    }
}