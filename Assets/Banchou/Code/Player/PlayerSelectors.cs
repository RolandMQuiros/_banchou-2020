using System;
using System.Linq;
using System.Collections.Generic;

using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Player {
    public static class PlayerSelectors {
        public static PlayerId NextPlayerId(this GameState state) {
            var existingIds = new HashSet<int>(state.Players.States.Keys.Select(p => p.Id));

            if (existingIds.Count == 0) {
                return new PlayerId(1);
            } else {
                var max = existingIds.Max() + 1;
                for (int i = 1; i <= max; i++) {
                    if (!existingIds.Contains(i)) {
                        return new PlayerId(i);
                    }
                }
            }

            return PlayerId.Empty;
        }

        public static PlayerState GetPlayer(this GameState state, PlayerId playerId) {
            PlayerState player;
            if (state.Players.States.TryGetValue(playerId, out player)) {
                return player;
            }
            return null;
        }

        public static string GetPlayerName(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Name;
        }

        public static PawnId GetPlayerPawn(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Pawn ?? PawnId.Empty;
        }

        public static string GetPlayerPrefabKey(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).PrefabKey;
        }

        public static Guid GetPlayerNetworkId(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.NetworkId ?? Guid.Empty;
        }

        public static bool IsLocalPlayer(this GameState state, PlayerId playerId) {
            return state.GetNetworkId() == state.GetPlayerNetworkId(playerId);
        }

        public static IDictionary<PlayerId, PlayerState> GetPlayers(this GameState state) {
            return state.Players.States;
        }

        public static IEnumerable<PlayerId> GetPlayerIds(this GameState state) {
            return state.Players.States.Keys;
        }
    }
}