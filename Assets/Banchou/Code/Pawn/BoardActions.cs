using Banchou.Player;

namespace Banchou.Pawn {
    namespace StateAction {
        public class Add {
            public PawnId PawnId;
            public PlayerId PlayerId;
            public string PrefabKey;
        }

        public class Remove {
            public PawnId PawnId;
        }
    }

    public class BoardActions {
        public StateAction.Add Add(PawnId pawnId, string prefabKey = null) => new StateAction.Add {
            PawnId = pawnId,
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey
        };

        public StateAction.Add Add(PawnId pawnId, PlayerId playerId, string prefabKey = null) => new StateAction.Add {
            PawnId = pawnId,
            PlayerId = playerId,
            PrefabKey = prefabKey
        };

        public StateAction.Remove Remove(PawnId pawnId) => new StateAction.Remove {
            PawnId = pawnId
        };
    }
}