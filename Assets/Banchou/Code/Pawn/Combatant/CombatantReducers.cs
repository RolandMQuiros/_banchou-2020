using Banchou.Pawn;

namespace Banchou.Combatant {
    public static class CombatantsReducers{
        public static CombatantsState Reduce(in CombatantsState prev, in object action) {
            var add = action as StateAction.Add;
            if (add != null) {
                CombatantState pawn;
                if (!prev.TryGetValue(add.PawnId, out pawn)) {
                    return new CombatantsState(prev) {
                        [add.PawnId] = new CombatantState {
                            Health = add.Health
                        }
                    };
                }
            }

            var remove = action as Pawn.StateAction.Remove;
            if (remove != null) {
                var next = new CombatantsState(prev);
                next.Remove(remove.PawnId);
                return next;
            }

            var combatantAction = action as StateAction.CombatantAction;
            if (combatantAction != null) {
                CombatantState prevCombatant;
                if (prev.TryGetValue(combatantAction.CombatantId, out prevCombatant)) {
                    return new CombatantsState(prev) {
                        [combatantAction.CombatantId] = ReduceCombatant(prevCombatant, combatantAction)
                    };
                }
            }

            var hit = action as StateAction.Hit;
            if (hit != null) {
                CombatantState from, to;
                if (prev.TryGetValue(hit.From, out from) && prev.TryGetValue(hit.To, out to)) {
                    var lastHit = new Hit {
                        From = hit.From,
                        To = hit.To,
                        Medium = hit.Medium,
                        Push = hit.Push,
                        Strength = hit.Strength,
                        When = hit.When
                    };

                    return new CombatantsState(prev) {
                        [hit.From] = new CombatantState(from) {
                            HitDealt = lastHit
                        },
                        [hit.To] = new CombatantState(to) {
                            HitTaken = lastHit
                        }
                    };
                }
            }

            return prev;
        }

        public static CombatantState ReduceCombatant(in CombatantState prev, in StateAction.CombatantAction action) {
            var lockOn = action as StateAction.LockOn;
            if (lockOn != null) {
                return new CombatantState(prev) {
                    LockOnTarget = lockOn.To
                };
            }

            var lockOff = action as StateAction.LockOff;
            if (lockOff != null) {
                return new CombatantState(prev) {
                    LockOnTarget = PawnId.Empty
                };
            }

            var pushCommand = action as StateAction.PushCommand;
            if (pushCommand != null) {
                return new CombatantState(prev) {
                    LastCommand = new PushedCommand() {
                        Command = pushCommand.Command,
                        When = pushCommand.When
                    }
                };
            }

            return prev;
        }
    }
}