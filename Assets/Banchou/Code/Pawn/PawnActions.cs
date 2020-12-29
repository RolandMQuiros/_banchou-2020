using UnityEngine;

namespace Banchou.Pawn {
    namespace StateAction {
        public interface IPawnAction {
            PawnId PawnId { get; }
        }

        public struct FSMStateChanged {
            public PawnId PawnId;
            public int StateHash;
            public bool IsLoop;
            public float ClipLength;
            public float When;
            public Vector3 Position;
            public Vector3 Forward;
        }
    }

    public class PawnActions {
        private PawnId _pawnId;

        public PawnActions(PawnId pawnId) {
            _pawnId = pawnId;
        }

        public StateAction.FSMStateChanged FSMStateChanged(
            int stateNameHash,
            float clipLength,
            bool isLoop,
            float when,
            Vector3 position,
            Vector3 forward
        ) => new StateAction.FSMStateChanged {
            PawnId = _pawnId,
            StateHash = stateNameHash,
            IsLoop = isLoop,
            ClipLength = clipLength,
            When = when,
            Position = position,
            Forward = forward
        };
    }
}