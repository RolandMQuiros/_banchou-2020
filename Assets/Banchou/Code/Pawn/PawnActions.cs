using UnityEngine;
using Banchou.Network;

namespace Banchou.Pawn {
    namespace StateAction {
        public interface IPawnAction {
            PawnId PawnId { get; }
            float When { get; }
        }
    }

    public class PawnActions {
        private PawnId _pawnId;
        private GetTime _getTime;
        public PawnActions(PawnId pawnId, GetTime getTime) {
            _pawnId = pawnId;
            _getTime = getTime;
        }
    }
}