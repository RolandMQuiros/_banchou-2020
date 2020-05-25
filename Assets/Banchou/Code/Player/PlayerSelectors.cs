using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Player {
    public static class PlayerSelectors {
        public static PlayerState GetPlayer(this GameState state, PlayerId playerId) {
            PlayerState player;
            if (state.Players.TryGetValue(playerId, out player)) {
                return player;
            }
            return null;
        }

        public static InputSource GetPlayerInputSource(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).Source;
        }

        public static Team GetPlayerTeam(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Team ?? Team.None;
        }

        public static IDictionary<PlayerId, PlayerState> GetPlayers(this GameState state) {
            return state.Players;
        }

        public static IEnumerable<PlayerId> GetPlayerIds(this GameState state) {
            return state.Players.Keys;
        }

        public static IEnumerable<PawnId> GetPlayerPawns(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Pawns ?? Enumerable.Empty<PawnId>();
        }

        public static bool DoesPlayerHavePawn(this GameState state, PlayerId playerId, PawnId pawnId) {
            var player = state.GetPlayer(playerId);
            return player != null && player.Pawns.Contains(pawnId);
        }

        public static IEnumerable<PawnId> GetPlayerSelectedPawns(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.SelectedPawns ?? Enumerable.Empty<PawnId>();
        }

        public static bool IsPlayerPawnSelected(this GameState state, PlayerId playerId, PawnId pawnId) {
            return state.GetPlayerSelectedPawns(playerId).Contains(pawnId);
        }

        public static IEnumerable<PawnId> GetPlayerTargets(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Targets ?? Enumerable.Empty<PawnId>();
        }

        public static PawnId GetPlayerLockOnTarget(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.LockOnTarget ?? PawnId.Empty;
        }

        public static Vector2 GetPlayerMovement(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.InputMovement ?? Vector3.zero;
        }

        public static Vector2 GetPlayerLook(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.InputLook ?? Vector2.zero;
        }

        public static PushedCommand GetLastPlayerCommand(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.LastCommand ?? PushedCommand.Empty;
        }
    }
}