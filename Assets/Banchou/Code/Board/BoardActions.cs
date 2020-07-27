﻿using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Board {
    namespace StateAction {
        public struct AddPawn {
            public PawnId PawnId;
            public PlayerId PlayerId;
            public string PrefabKey;
        }

        public struct RemovePawn {
            public PawnId PawnId;
        }

        public struct AddScene {
            public string Scene;
        }

        public struct SetScene {
            public string Scene;
        }

        public struct SceneLoaded {
            public string Scene;
        }
    }

    public class BoardActions {
        public StateAction.AddPawn AddPawn(PawnId pawnId, string prefabKey = null) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey
        };

        public StateAction.AddPawn AddPawn(PawnId pawnId, PlayerId playerId, string prefabKey = null) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = playerId,
            PrefabKey = prefabKey
        };

        public StateAction.RemovePawn RemovePawn(PawnId pawnId) => new StateAction.RemovePawn {
            PawnId = pawnId
        };

        public StateAction.AddScene AddScene(string sceneName) => new StateAction.AddScene {
            Scene = sceneName
        };

        public StateAction.SetScene SetScene(string sceneName) => new StateAction.SetScene {
            Scene = sceneName
        };

        public StateAction.SceneLoaded SceneLoaded(string sceneName) => new StateAction.SceneLoaded {
            Scene = sceneName
        };
    }
}