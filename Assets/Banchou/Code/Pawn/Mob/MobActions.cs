using UnityEngine;

using Banchou.Pawn;
using Banchou.Network;

namespace Banchou.Mob {
     namespace StateAction {
        public struct AddMob {
            public PawnId PawnId;
            public float When;
        }

        public struct RemoveMob {
            public PawnId PawnId;
            public float When;
        }

        public interface IMobAction {
            PawnId PawnId { get; }
            float When { get; }
        }

        public struct MobApproachTarget : IMobAction {
            public PawnId PawnId { get; set; }
            public PawnId TargetId;
            public float StoppingDistance;
            public float When { get; set; }
        }

        public struct MobApproachPosition : IMobAction {
            public PawnId PawnId { get; set; }
            public Vector3 Position;
            public float StoppingDistance;
            public float When { get; set; }
        }

        public struct MobApproachCompleted : IMobAction {
            public PawnId PawnId { get; set; }
            public float When { get; set; }
        }

        public struct MobApproachInterrupted : IMobAction {
            public PawnId PawnId { get; set; }
            public float When { get; set; }
        }
    }

    public class MobActions {
        private GetServerTime _getServerTime;

        public MobActions(GetServerTime getServerTime) {
            _getServerTime = getServerTime;
        }

        public StateAction.AddMob Add(PawnId pawnId, float? when = null) {
            return new StateAction.AddMob {
                PawnId = pawnId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.RemoveMob Remove(PawnId pawnId, float? when = null) {
            return new StateAction.RemoveMob {
                PawnId = pawnId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.MobApproachTarget ApproachTarget(PawnId pawnId, PawnId targetId, float stoppingDistance, float? when = null) {
            return new StateAction.MobApproachTarget {
                PawnId = pawnId,
                TargetId = targetId,
                StoppingDistance = stoppingDistance,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.MobApproachPosition ApproachPosition(PawnId pawnId, Vector3 position, float? when = null) {
            return new StateAction.MobApproachPosition {
                PawnId = pawnId,
                Position = position,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.MobApproachInterrupted ApproachInterrupted(PawnId pawnId, float? when = null) {
            return new StateAction.MobApproachInterrupted {
                PawnId = pawnId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.MobApproachCompleted ApproachCompleted(PawnId pawnId, float? when = null) {
            return new StateAction.MobApproachCompleted {
                PawnId = pawnId,
                When = when ?? _getServerTime()
            };
        }
    }
}