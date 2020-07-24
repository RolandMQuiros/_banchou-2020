using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Banchou.Player;

namespace Banchou.Pawn {
    public static class PawnSelectors {
        public static bool HasPawn(this GameState state, PawnId pawnId) {
            return state.Pawns.ContainsKey(pawnId);
        }

        public static IEnumerable<PawnState> GetPawns(this GameState state) {
            return state.Pawns.Values;
        }

        public static IEnumerable<PawnId> GetPawnIds(this GameState state) {
            return state.Pawns.Keys;
        }

        public static PawnState GetPawn(this GameState state, PawnId pawnId) {
            PawnState pawn;
            if (state.Pawns.TryGetValue(pawnId, out pawn)) {
                return pawn;
            }
            return null;
        }

        public static string GetPawnPrefabKey(this GameState state, PawnId pawnId) {
            return state.GetPawn(pawnId)?.PrefabKey;
        }

        public static float GetPawnTimeScale(this GameState state, PawnId pawnId) {
            return state.GetPawn(pawnId)?.TimeScale ?? 1f;
        }

        public static PlayerId GetPawnPlayerId(this GameState state, PawnId pawnId) {
            return state.GetPawn(pawnId)?.PlayerId ?? PlayerId.Empty;
        }

        public static PlayerState GetPawnPlayer(this GameState state, PawnId pawnId) {
            return state.GetPlayer(state.GetPawnPlayerId(pawnId));
        }

        public static IEnumerable<PawnId> GetPawnPlayerTargets(this GameState state, PawnId pawnId) {
            return state.GetPlayer(state.GetPawnPlayerId(pawnId))?.Targets ?? Enumerable.Empty<PawnId>();
        }

        public static PawnRollbackState GetPawnRollbackState(this GameState state, PawnId pawnId) {
            return state.GetPawn(pawnId).RollbackState;
        }
    }
}