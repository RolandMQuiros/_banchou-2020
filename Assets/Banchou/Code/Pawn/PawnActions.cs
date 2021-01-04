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
        private GetServerTime _getServerTime;
        public PawnActions(PawnId pawnId, GetServerTime getServerTime) {
            _pawnId = pawnId;
            _getServerTime = getServerTime;
        }
    }
}