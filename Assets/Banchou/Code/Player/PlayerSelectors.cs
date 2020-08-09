﻿using System.Linq;
using System.Collections.Generic;

using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Player {
    public static class PlayerSelectors {
        public static PlayerId CreatePlayerId(this GameState state) {
            var existingIds = new HashSet<int>(state.Pawns.States.Keys.Select(p => p.Id));

            if (existingIds.Count == 0) {
                return new PlayerId(1);
            } else {
                for (int i = 1; i != existingIds.Max(); i++) {
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

        public static PawnId GetPlayerPawn(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId)?.Pawn ?? PawnId.Empty;
        }

        public static string GetPlayerPrefabKey(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).PrefabKey;
        }

        public static NetworkInfo GetPlayerNetworkInfo(this GameState state, PlayerId playerId) {
            return state.GetPlayer(playerId).NetworkInfo;
        }

        public static bool IsLocalPlayer(this GameState state, PlayerId playerId) {
            var ip = state.GetIP();
            var playerNetInfo = state.GetPlayerNetworkInfo(playerId);
            return playerNetInfo.IP == ip;
        }

        public static IDictionary<PlayerId, PlayerState> GetPlayers(this GameState state) {
            return state.Players.States;
        }

        public static IEnumerable<PlayerId> GetPlayerIds(this GameState state) {
            return state.Players.States.Keys;
        }
    }
}