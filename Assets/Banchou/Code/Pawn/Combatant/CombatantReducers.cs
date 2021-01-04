using System.Collections.Generic;
using System.Linq;
using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantsReducers{
        public static CombatantsState Reduce(in CombatantsState prev, in object action) {
            if (action is StateAction.AddCombatant add) {
                CombatantState pawn;
                if (!prev.States.TryGetValue(add.PawnId, out pawn)) {
                    return new CombatantsState(prev) {
                        States = new Dictionary<PawnId, CombatantState>(prev.States) {
                            [add.PawnId] = new CombatantState {
                                Health = add.Health
                            }
                        },
                        LastUpdated = add.When
                    };
                }
            }

            if (action is Board.StateAction.RemovePawn remove) {
                var next = new CombatantsState(prev) {
                    States = new Dictionary<PawnId, CombatantState>(prev.States),
                    LastUpdated = remove.When
                };
                next.States.Remove(remove.PawnId);
                return next;
            }

            if (action is StateAction.ICombatantAction combatantAction) {
                CombatantState prevCombatant;
                if (prev.States.TryGetValue(combatantAction.CombatantId, out prevCombatant)) {
                    return new CombatantsState(prev) {
                        States = new Dictionary<PawnId, CombatantState>(prev.States) {
                            [combatantAction.CombatantId] = ReduceCombatant(prevCombatant, combatantAction)
                        },
                        LastUpdated = combatantAction.When
                    };
                }
            }

            if (action is StateAction.Hit hit) {
                CombatantState from, to;
                if (prev.States.TryGetValue(hit.From, out from) && prev.States.TryGetValue(hit.To, out to)) {
                    var listHit = new Hit {
                        From = hit.From,
                        To = hit.To,
                        Medium = hit.Medium,
                        Push = hit.Push,
                        Strength = hit.Strength,
                        When = hit.When
                    };

                    return new CombatantsState(prev) {
                        States = new Dictionary<PawnId, CombatantState>(prev.States) {
                            [hit.From] = new CombatantState(from) {
                                HitDealt = listHit
                            },
                            [hit.To] = new CombatantState(to) {
                                HitTaken = listHit
                            }
                        },
                        LastUpdated = hit.When
                    };
                }
            }

            return prev;
        }

        private static CombatantState ReduceCombatant(in CombatantState prev, in StateAction.ICombatantAction action) {
            if (action is StateAction.AddTarget addTarget) {
                return new CombatantState(prev) {
                    Targets = prev.Targets.Append(addTarget.Target)
                };
            }

            if (action is StateAction.RemoveTarget removeTarget) {
                return new CombatantState(prev) {
                    Targets = prev.Targets.Where(targetId => targetId != removeTarget.Target)
                };
            }

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