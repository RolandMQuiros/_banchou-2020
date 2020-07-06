using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Mob {
     namespace StateAction {
        public class AddMob {
            public PawnId PawnId;
        }

        public class RemoveMob {
            public PawnId PawnId;
        }

        public class MobAction {
            public PawnId PawnId;
        }

        public class MobApproachTarget : MobAction {
            public PawnId TargetId;
            public float StoppingDistance;
        }

        public class MobApproachPosition : MobAction {
            public Vector3 Position;
            public float StoppingDistance;
        }

        public class MobApproachCompleted : MobAction { }

        public class MobApproachInterrupted : MobAction { }
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