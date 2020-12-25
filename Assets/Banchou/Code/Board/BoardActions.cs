using Redux;
using UnityEngine;

using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Board {
    namespace StateAction {
        public struct AddPawn {
            public PawnId PawnId;
            public PlayerId PlayerId;
            public string PrefabKey;
            public Vector3 SpawnPosition;
            public Quaternion SpawnRotation;
        }

        public struct RemovePawn {
            public PawnId PawnId;
        }

        public struct ClearPawns { }

        public struct AddScene {
            public string Scene;
        }

        public struct SetScene {
            public string Scene;
        }

        public struct SceneLoaded {
            public string Scene;
        }

        public struct InjectionsFinished { }
    }

    public class BoardActions {
        public ActionsCreator<GameState> AddPawn(
            string prefabKey,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion)
        ) => (dispatch, getState) => {
            dispatch(
                new StateAction.AddPawn {
                    PawnId = getState().NextPawnId(),
                    PlayerId = PlayerId.Empty,
                    PrefabKey = prefabKey,
                    SpawnPosition = position,
                    SpawnRotation = rotation
                }
            );
        };

        public StateAction.AddPawn AddPawn(
            PawnId pawnId,
            string prefabKey = null,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion)
        ) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey,
            SpawnPosition = position,
            SpawnRotation = rotation
        };

        public StateAction.AddPawn AddPawn(
            PawnId pawnId,
            PlayerId playerId,
            string prefabKey = null,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion)
        ) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = playerId,
            PrefabKey = prefabKey,
            SpawnPosition = position,
            SpawnRotation = rotation
        };

        public StateAction.RemovePawn RemovePawn(PawnId pawnId) => new StateAction.RemovePawn {
            PawnId = pawnId
        };

        public StateAction.ClearPawns ClearPawns() => new StateAction.ClearPawns();

        public StateAction.AddScene AddScene(string sceneName) => new StateAction.AddScene {
            Scene = sceneName
        };

        public StateAction.SetScene SetScene(string sceneName) => new StateAction.SetScene {
            Scene = sceneName
        };

        public StateAction.SceneLoaded SceneLoaded(string sceneName) => new StateAction.SceneLoaded {
            Scene = sceneName
        };

        public StateAction.InjectionsFinished InjectionsFinished() => new StateAction.InjectionsFinished();
    }
}