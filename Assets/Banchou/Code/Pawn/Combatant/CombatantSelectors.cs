﻿using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantSelectors {
        public static CombatantState GetCombatant(this GameState state, PawnId combatantId) {
            CombatantState combatant;
            if (state.Combatants.States.TryGetValue(combatantId, out combatant)) {
                return combatant;
            }
            return null;
        }

        public static bool IsCombatant(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId) != null;
        }

        public static PawnId GetCombatantLockOnTarget(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.LockOnTarget ?? PawnId.Empty;
        }

        public static IEnumerable<PawnId> GetCombatantTargets(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.Targets ?? Enumerable.Empty<PawnId>();
        }

        public static IEnumerable<PawnId> GetCombatantIds(this GameState state) {
            return state.Combatants.States.Keys;
        }

        public static IEnumerable<CombatantState> GetCombatants(this GameState state) {
            return state.Combatants.States.Values;
        }

        public static int GetCombatantHealth(this GameState state, PawnId combatantId) {
            CombatantState combatantState;
            if (state.Combatants.States.TryGetValue(combatantId, out combatantState)) {
                return combatantState.Health;
            }
            return 0;
        }

        public static Hit GetCombatantHitTaken(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.HitTaken;
        }

        public static Hit GetCombatantHitDealt(this GameState state, PawnId combatantId) {
            return state.GetCombatant(combatantId)?.HitDealt;
        }
    }
}