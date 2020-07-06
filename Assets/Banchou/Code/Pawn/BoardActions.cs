using Banchou.Player;

namespace Banchou.Pawn {
    namespace StateAction {
        public class AddPawn {
            public PawnId PawnId;
            public PlayerId PlayerId;
            public string PrefabKey;
        }

        public class RemovePawn {
            public PawnId PawnId;
        }
    }

    public class BoardActions {
        public StateAction.AddPawn Add(PawnId pawnId, string prefabKey = null) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey
        };

        public StateAction.AddPawn Add(PawnId pawnId, PlayerId playerId, string prefabKey = null) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = playerId,
            PrefabKey = prefabKey
        };

        public StateAction.RemovePawn Remove(PawnId pawnId) => new StateAction.RemovePawn {
            PawnId = pawnId
        };
    }
}