using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Mob {
     namespace StateAction {
        public struct AddMob {
            public PawnId PawnId;
        }

        public struct RemoveMob {
            public PawnId PawnId;
        }

        public interface IMobAction {
            PawnId PawnId { get; }
        }

        public struct MobApproachTarget : IMobAction {
            public PawnId PawnId { get; set; }
            public PawnId TargetId;
            public float StoppingDistance;
        }

        public struct MobApproachPosition : IMobAction {
            public PawnId PawnId { get; set; }
            public Vector3 Position;
            public float StoppingDistance;
        }

        public struct MobApproachCompleted : IMobAction {
            public PawnId PawnId { get; set; }
        }

        public struct MobApproachInterrupted : IMobAction {
            public PawnId PawnId { get; set; }
        }
    }

    public class MobActions {
        public StateAction.AddMob Add(PawnId pawnId) {
            return new StateAction.AddMob {
                PawnId = pawnId
            };
        }

        public StateAction.RemoveMob Remove(PawnId pawnId) {
            return new StateAction.RemoveMob {
                PawnId = pawnId
            };
        }

        public StateAction.MobApproachTarget ApproachTarget(PawnId pawnId, PawnId targetId, float stoppingDistance) {
            return new StateAction.MobApproachTarget {
                PawnId = pawnId,
                TargetId = targetId,
                StoppingDistance = stoppingDistance
            };
        }

        public StateAction.MobApproachPosition ApproachPosition(PawnId pawnId, Vector3 position) {
            return new StateAction.MobApproachPosition {
                PawnId = pawnId,
                Position = position
            };
        }

        public StateAction.MobApproachInterrupted ApproachInterrupted(PawnId pawnId) {
            return new StateAction.MobApproachInterrupted {
                PawnId = pawnId
            };
        }

        public StateAction.MobApproachCompleted ApproachCompleted(PawnId pawnId) {
            return new StateAction.MobApproachCompleted {
                PawnId = pawnId
            };
        }
    }
}