using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Mob {
     namespace StateAction {
        public class Add {
            public PawnId PawnId;
        }

        public class Remove {
            public PawnId PawnId;
        }

        public class MobAction {
            public PawnId PawnId;
        }

        public class ApproachTarget : MobAction {
            public PawnId TargetId;
            public float StoppingDistance;
        }

        public class ApproachPosition : MobAction {
            public Vector3 Position;
            public float StoppingDistance;
        }

        public class ApproachCompleted : MobAction { }

        public class ApproachInterrupted : MobAction { }
    }

    public class MobActions {
        public StateAction.Add Add(PawnId pawnId) {
            return new StateAction.Add {
                PawnId = pawnId
            };
        }

        public StateAction.Remove Remove(PawnId pawnId) {
            return new StateAction.Remove {
                PawnId = pawnId
            };
        }

        public StateAction.ApproachTarget ApproachTarget(PawnId pawnId, PawnId targetId, float stoppingDistance) {
            return new StateAction.ApproachTarget {
                PawnId = pawnId,
                TargetId = targetId,
                StoppingDistance = stoppingDistance
            };
        }

        public StateAction.ApproachPosition ApproachPosition(PawnId pawnId, Vector3 position) {
            return new StateAction.ApproachPosition {
                PawnId = pawnId,
                Position = position
            };
        }

        public StateAction.ApproachInterrupted ApproachInterrupted(PawnId pawnId) {
            return new StateAction.ApproachInterrupted {
                PawnId = pawnId
            };
        }

        public StateAction.ApproachCompleted ApproachCompleted(PawnId pawnId) {
            return new StateAction.ApproachCompleted {
                PawnId = pawnId
            };
        }
    }
}