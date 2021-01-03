﻿using Redux;
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

        public struct SyncBoard {
            public BoardState Board;
        }

        public struct RollbackBoard {
            public BoardState Board;
        }
        public struct ResimulateBoard { }
        public struct CompleteBoardRollback { }
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

        public StateAction.RemovePawn RemovePawn(PawnId pawnId) => new StateAction.RemovePawn { PawnId = pawnId };
        public StateAction.ClearPawns ClearPawns() => new StateAction.ClearPawns();
        public StateAction.SyncBoard Sync(BoardState board) => new StateAction.SyncBoard { Board = board };
        public StateAction.RollbackBoard Rollback(BoardState board) => new StateAction.RollbackBoard { Board = board };
        public StateAction.ResimulateBoard Resimulate() => new StateAction.ResimulateBoard();
        public StateAction.CompleteBoardRollback CompleteRollback() => new StateAction.CompleteBoardRollback();
    }
}