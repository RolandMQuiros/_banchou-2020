using UnityEngine;

namespace Banchou.Pawn {
    namespace StateAction {
        public interface IPawnAction {
            PawnId PawnId { get; }
        }

        public struct FSMStateChanged {
            public PawnId PawnId;
            public int Statehash;
            public bool IsLoop;
            public float ClipLength;
            public float When;
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
    }
}