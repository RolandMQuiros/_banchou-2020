using MessagePack;
using UnityEngine;

namespace Banchou.Pawn {
    namespace StateAction {
        public interface IPawnAction {
            PawnId PawnId { get; }
        }

        [MessagePackObject]
        public struct FSMStateChanged {
            [Key(0)] public PawnId PawnId;
            [Key(1)] public int Statehash;
            [Key(2)] public bool IsLoop;
            [Key(3)] public float ClipLength;
            [Key(4)] public float When;
        }

        [MessagePackObject]
        public struct RollbackStarted : IPawnAction {
            [Key(0)] public PawnId PawnId { get; set; }
        }

        [MessagePackObject]
        public struct FastForwarding : IPawnAction {
            [Key(0)] public PawnId PawnId { get; set; }
            [Key(1)] public float CorrectionTime;
        }

        [MessagePackObject]
        public struct RollbackComplete : IPawnAction {
            [Key(0)] public PawnId PawnId { get; set; }
        }

        [MessagePackObject]
        public struct SyncPawn : IPawnAction {
            [Key(0)] public PawnId PawnId { get; set; }
            [Key(1)] public Vector3 Position;
            [Key(2)] public Quaternion Rotation;
            [Key(3)] public int StateHash;
            [Key(4)] public float NormalizedTime;
        }
    }

    public class PawnActions {
        private PawnId _pawnId;

        public PawnActions(PawnId pawnId) {
            _pawnId = pawnId;
        }

        public StateAction.FSMStateChanged FSMStateChanged(int stateNameHash, float clipLength, bool isLoop, float when) => new StateAction.FSMStateChanged {
            PawnId = _pawnId,
            Statehash = stateNameHash,
            IsLoop = isLoop,
            ClipLength = clipLength,
            When = when
        };

        public StateAction.SyncPawn SyncPawn(Vector3 position, Quaternion rotation) => new StateAction.SyncPawn {
            PawnId = _pawnId,
            Position = position,
            Rotation = rotation
        };

        public StateAction.SyncPawn SyncPawn(Vector3 position, Quaternion rotation, int stateHash, float normalizedTime) => new StateAction.SyncPawn {
            PawnId = _pawnId,
            Position = position,
            Rotation = rotation,
            StateHash = stateHash,
            NormalizedTime = normalizedTime
        };

        public StateAction.RollbackStarted RollbackStarted() => new StateAction.RollbackStarted { PawnId = _pawnId };
        public StateAction.FastForwarding FastForwarding(float correctionTime) => new StateAction.FastForwarding {
            PawnId = _pawnId,
            CorrectionTime = correctionTime
        };
        public StateAction.RollbackComplete RollbackComplete() => new StateAction.RollbackComplete { PawnId = _pawnId };
    }
}