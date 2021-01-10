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
            public float When;
        }

        public struct RemovePawn {
            public PawnId PawnId;
            public float When;
        }

        public struct ClearPawns {
            public float When;
        }

        public struct SyncBoard {
            public BoardState Board;
            public float When;
        }

        [LocalAction]
        public struct RollbackBoard {
            public BoardState Board;
        }

        public struct SyncPawn {
            public PawnFrameData Frame;
            public float When;
        }
    }

    public class BoardActions {
        private GetTime _getTime;
        public BoardActions(GetTime getTime) {
            _getTime = getTime;
        }

        public ActionsCreator<GameState> AddPawn(
            string prefabKey,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion),
            float? when = null
        ) => (dispatch, getState) => {
            dispatch(
                new StateAction.AddPawn {
                    PawnId = getState().NextPawnId(),
                    PlayerId = PlayerId.Empty,
                    PrefabKey = prefabKey,
                    SpawnPosition = position,
                    SpawnRotation = rotation,
                    When = when ?? _getTime()
                }
            );
        };

        public StateAction.AddPawn AddPawn(
            PawnId pawnId,
            string prefabKey = null,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion),
            float? when = null
        ) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = PlayerId.Empty,
            PrefabKey = prefabKey,
            SpawnPosition = position,
            SpawnRotation = rotation,
            When = when ?? _getTime()
        };

        public StateAction.AddPawn AddPawn(
            PawnId pawnId,
            PlayerId playerId,
            string prefabKey = null,
            Vector3 position = default(Vector3),
            Quaternion rotation = default(Quaternion),
            float? when = null
        ) => new StateAction.AddPawn {
            PawnId = pawnId,
            PlayerId = playerId,
            PrefabKey = prefabKey,
            SpawnPosition = position,
            SpawnRotation = rotation,
            When = when ?? _getTime()
        };

        public StateAction.RemovePawn RemovePawn(PawnId pawnId, float? when = null) => new StateAction.RemovePawn { PawnId = pawnId, When = when ?? _getTime() };
        public StateAction.ClearPawns ClearPawns(float? when = null) => new StateAction.ClearPawns { When = when ?? _getTime() };
        public StateAction.SyncBoard Sync(BoardState board, float? when = null) => new StateAction.SyncBoard { Board = board, When = when ?? _getTime() };
        public StateAction.RollbackBoard Rollback(BoardState board) => new StateAction.RollbackBoard { Board = board };
        public StateAction.SyncPawn SyncPawn(PawnFrameData frame, float? when = null) => new StateAction.SyncPawn { Frame = frame, When = when ?? _getTime() };
    }
}