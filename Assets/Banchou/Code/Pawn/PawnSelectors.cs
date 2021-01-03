using System.Linq;
using System.Collections.Generic;
using Banchou.Player;

namespace Banchou.Pawn {
    public static class PawnSelectors {
        public static IDictionary<PawnId, PawnState> GetPawns(this GameState state) {
            return state.Board.Pawns.States;
        }

        public static PawnId NextPawnId(this GameState state) {
            var existingIds = new HashSet<int>(state.GetPawns().Keys.Select(p => p.Id));

            if (existingIds.Count == 0) {
                return new PawnId(1);
            } else {
                var max = existingIds.Max() + 1;
                for (int i = 1; i <= max; i++) {
                    if (!existingIds.Contains(i)) {
                        return new PawnId(i);
                    }
                }
            }

            return PawnId.Empty;
        }

        public static bool HasPawn(this GameState state, PawnId pawnId) {
            return state.GetPawns().ContainsKey(pawnId);
        }

        public static IEnumerable<PawnId> GetPawnIds(this GameState state) {
            return state.GetPawns().Keys;
        }

        public static PawnState GetPawn(this GameState state, PawnId pawnId) {
            PawnState pawn;
            if (state.GetPawns().TryGetValue(pawnId, out pawn)) {
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

        public static PawnFSMState GetLatestFSMChange(this GameState state) {
            return state.Board.Pawns.LatestFSMChange;
        }
    }
}