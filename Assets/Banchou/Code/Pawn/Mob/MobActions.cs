using MessagePack;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Mob {
     namespace StateAction {
        [MessagePackObject]
        public struct AddMob {
            [Key(0)] public PawnId PawnId;
        }

        [MessagePackObject]
        public struct RemoveMob {
            [Key(0)] public PawnId PawnId;
        }

        public interface IMobAction {
            [Key(0)] PawnId PawnId { get; }
        }

        [MessagePackObject]
        public struct MobApproachTarget : IMobAction {
            [Key(0)] public PawnId PawnId { get; set; }
            [Key(1)] public PawnId TargetId;
            [Key(2)] public float StoppingDistance;
        }

        [MessagePackObject]
        public struct MobApproachPosition : IMobAction {
            [Key(0)] public PawnId PawnId { get; set; }
            [Key(1)] public Vector3 Position;
            [Key(2)] public float StoppingDistance;
        }

        [MessagePackObject]
        public struct MobApproachCompleted : IMobAction {
            [Key(0)] public PawnId PawnId { get; set; }
        }

        [MessagePackObject]
        public struct MobApproachInterrupted : IMobAction {
            [Key(0)] public PawnId PawnId { get; set; }
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