using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;
using Banchou.Combatant;

namespace Banchou.Player {
    public static class PlayerSelectors {
        public static PlayerState GetPlayer(this GameState state, PlayerId playerId) {
            PlayerState player;
            if (state.Players.TryGetValue(playerId, out player)) {
                return player;
            }
            return null;
        }

        public static PawnId GetPlayerPawn(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Pawn ?? PawnId.Empty;
        }

        public static InputSource GetPlayerInputSource(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).Source;
        }

        public static IDictionary<PlayerId, PlayerState> GetPlayers(this GameState state) {
            return state.Players;
        }

        public static IEnumerable<PlayerId> GetPlayerIds(this GameState state) {
            return state.Players.Keys;
        }

        public static IEnumerable<PawnId> GetPlayerTargets(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Targets ?? Enumerable.Empty<PawnId>();
        }


        public static Vector2 GetPlayerMovement(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.InputMovement ?? Vector3.zero;
        }

        public static Vector2 GetPlayerLook(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.InputLook ?? Vector2.zero;
        }
    }
}