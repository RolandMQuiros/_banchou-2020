using UnityEngine;

namespace Banchou.Pawn {
    namespace StateAction {
        public interface IPawnAction {
            PawnId PawnId { get; }
        }
    }

    public class PawnActions {
        private PawnId _pawnId;

        public PawnActions(PawnId pawnId) {
            _pawnId = pawnId;
        }
    }
}