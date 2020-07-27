using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantsReducers{
        public static CombatantsState Reduce(in CombatantsState prev, in object action) {
            if (action is StateAction.AddCombatant add) {
                CombatantState pawn;
                if (!prev.TryGetValue(add.PawnId, out pawn)) {
                    return new CombatantsState(prev) {
                        [add.PawnId] = new CombatantState {
                            Health = add.Health
                        }
                    };
                }
            }

            if (action is Board.StateAction.RemovePawn remove) {
                var next = new CombatantsState(prev);
                next.Remove(remove.PawnId);
                return next;
            }

            if (action is StateAction.ICombatantAction combatantAction) {
                CombatantState prevCombatant;
                if (prev.TryGetValue(combatantAction.CombatantId, out prevCombatant)) {
                    return new CombatantsState(prev) {
                        [combatantAction.CombatantId] = ReduceCombatant(prevCombatant, combatantAction)
                    };
                }
            }

            if (action is StateAction.Hit hit) {
                CombatantState from, to;
                if (prev.TryGetValue(hit.From, out from) && prev.TryGetValue(hit.To, out to)) {
                    var listHit = new Hit {
                        From = hit.From,
                        To = hit.To,
                        Medium = hit.Medium,
                        Push = hit.Push,
                        Strength = hit.Strength,
                        When = hit.When
                    };

                    return new CombatantsState(prev) {
                        [hit.From] = new CombatantState(from) {
                            HitDealt = listHit
                        },
                        [hit.To] = new CombatantState(to) {
                            HitTaken = listHit
                        }
                    };
                }
            }

            return prev;
        }

        private static CombatantState ReduceCombatant(in CombatantState prev, in StateAction.ICombatantAction action) {
            if (action is StateAction.LockOn lockOn) {
                return new CombatantState(prev) {
                    LockOnTarget = lockOn.To
                };
            }

            if (action is StateAction.LockOff lockOff) {
                return new CombatantState(prev) {
                    LockOnTarget = PawnId.Empty
                };
            }

            return prev;
        }
    }
}