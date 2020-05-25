using System.Linq;
using Banchou.Pawn;

namespace Banchou.Player.Activation {
    public static class ActivationSelectors {
        public static PawnId GetPlayerLeftPawn(this GameState state, PlayerId playerId) {
            var pawns = state.GetPlayerPawns(playerId);
            return pawns.ElementAtOrDefault(1);
        }

        public static bool IsPlayerLeftPawnActivated(this GameState state, PlayerId playerId) {
            return state.GetPlayerSelectedPawns(playerId)
                .Contains(state.GetPlayerLeftPawn(playerId));
        }

        public static PawnId GetPlayerRightPawn(this GameState state, PlayerId playerId) {
            var pawns = state.GetPlayerPawns(playerId);
            return pawns.ElementAtOrDefault(2);
        }

        public static bool IsPlayerRightPawnActivated(this GameState state, PlayerId playerId) {
            return state.GetPlayerSelectedPawns(playerId)
                .Contains(state.GetPlayerRightPawn(playerId));
        }
    }
}