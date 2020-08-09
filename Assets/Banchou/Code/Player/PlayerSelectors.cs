using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Player {
    public static class PlayerSelectors {
        public static PlayerState GetPlayer(this GameState state, PlayerId playerId) {
            PlayerState player;
            if (state.Players.States.TryGetValue(playerId, out player)) {
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

        public static NetworkInfo GetPlayerNetworkInfo(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).NetworkInfo;
        }

        public static IDictionary<PlayerId, PlayerState> GetPlayers(this GameState state) {
            return state.Players.States;
        }

        public static IEnumerable<PlayerId> GetPlayerIds(this GameState state) {
            return state.Players.States.Keys;
        }
    }
}