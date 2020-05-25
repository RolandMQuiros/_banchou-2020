using System.Collections.Generic;

using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantSelectors {
        public static CombatantState GetCombatant(this GameState state, PawnId pawnId) {
            CombatantState combatant;
            if (state.Combatants.TryGetValue(pawnId, out combatant)) {
                return combatant;
            }
            return null;
        }

        public static bool IsCombatant(this GameState state, PawnId pawnId) {
            return state.GetCombatant(pawnId) != null;
        }

        public static IEnumerable<PawnId> GetCombatantIds(this GameState state) {
            return state.Combatants.Keys;
        }

        public static IEnumerable<CombatantState> GetCombatants(this GameState state) {
            return state.Combatants.Values;
        }

        public static PushedCommand GetCombatantLastCommand(this GameState state, PawnId pawnId) {
            return state.GetPawnPlayer(pawnId)?.LastCommand ?? PushedCommand.Empty;
        }

        public static int GetCombatantHealth(this GameState state, PawnId pawnId) {
            CombatantState combatant;
            if (state.Combatants.TryGetValue(pawnId, out combatant)) {
                return combatant.Health;
            }
            return 0;
        }
    }
}