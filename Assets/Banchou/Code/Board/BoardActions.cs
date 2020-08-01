using MessagePack;
using UnityEngine;

using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Board {
    namespace StateAction {
        [MessagePackObject]
        public struct AddPawn {
            [Key(0)] public PawnId PawnId;
            [Key(1)] public PlayerId PlayerId;
            [Key(2)] public string PrefabKey;
            [Key(3)] public Vector3 SpawnPosition;
            [Key(4)] public Quaternion SpawnRotation;
        }

        [MessagePackObject]
        public struct RemovePawn {
            [Key(0)] public PawnId PawnId;
        }

        [MessagePackObject]
        public struct AddScene {
            [Key(0)] public string Scene;
        }

        [MessagePackObject]
        public struct SetScene {
            [Key(0)] public string Scene;
        }

        [MessagePackObject]
        public struct SceneLoaded {
            [Key(0)] public string Scene;
        }

        [MessagePackObject]
        public struct InjectionsFinished { }
    }

    public class BoardActions {
        public StateAction.AddPawn AddPawn(
            string prefabKey,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion)
        ) => new StateAction.AddPawn {
            PawnId = PawnId.Create(),
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey,
            SpawnPosition = position,
            SpawnRotation = rotation
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