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

        public static Vector2 GetPawnPlayerInputMovement(this GameState state, PawnId pawnId) {
            return state.GetPawnPlayer(pawnId)?.InputMovement ?? Vector2.zero;
        }

        public static PushedCommand? GetPawnPlayerCommand(this GameState state, PawnId pawnId) {
            return state.GetPawnPlayer(pawnId)?.LastCommand;
        }

        public static Vector2 GetPawnPlayerStick(this GameState state, PawnId pawnId) {
            return state.GetPawnPlayer(pawnId)?.InputMovement ?? Vector2.zero;
        }

        public static Team GetPawnPlayerTeam(this GameState state, PawnId pawnId) {
            return state.GetPawnPlayer(pawnId)?.Team ?? Team.None;
        }

        public static bool ArePawnsHostile(this GameState state, PawnId first, PawnId second) {
            return state.GetPawnPlayerTeam(first).IsHostile(state.GetPawnPlayerTeam(second));
        }

        public static bool IsPawnHostile(this GameState state, PlayerId playerId, PawnId pawnId) {
            return state.GetPlayerTeam(playerId).IsHostile(state.GetPawnPlayerTeam(pawnId));
        }

        public static bool IsPawnSelected(this GameState state, PawnId pawnId) {
            var player = state.GetPawnPlayer(pawnId);
            return player != null && player.SelectedPawns.Contains(pawnId);
        }
    }
}