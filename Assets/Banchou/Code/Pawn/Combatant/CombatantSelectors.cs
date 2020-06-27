using System.Collections.Generic;
using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantSelectors {
        public static CombatantState GetCombatant(this GameState state, PawnId combatantId) {
            CombatantState combatant;
            if (state.Combatants.TryGetValue(combatantId, out combatant)) {
                return combatant;
            }
            return null;
        }

        public static bool IsCombatant(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId) != null;
        }

        public static PawnId GetCombatantTarget(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.LockOnTarget ?? PawnId.Empty;
        }

        public static IEnumerable<PawnId> GetCombatantIds(this GameState state) {
            return state.Combatants.Keys;
        }

        public static IEnumerable<CombatantState> GetCombatants(this GameState state) {
            return state.Combatants.Values;
        }

        public static PushedCommand GetCombatantLastCommand(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.LastCommand ?? PushedCommand.Empty;
        }

        public static int GetCombatantHealth(this GameState state, PawnId combatantId) {
            CombatantState combatantState;
            if (state.Combatants.TryGetValue(combatantId, out combatantState)) {
                return combatantState.Health;
            }
            return 0;
        }

        public static Hit GetCombatantLastHit(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.LastHit ?? Hit.Empty;
        }
    }
}