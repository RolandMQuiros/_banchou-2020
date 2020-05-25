using System.Linq;

using Banchou.Pawn;
using Banchou.Player;

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

            var attach = action as Player.StateAction.Attach;
            if (attach != null) {
                var next = new CombatantsState(prev);
                foreach (var pawnId in attach.Pawns) {
                    CombatantState prevCombatant;
                    if (prev.TryGetValue(pawnId, out prevCombatant)) {
                        next[pawnId] = new CombatantState(prevCombatant) {
                            Player = attach.PlayerId
                        };
                    }
                }
                return next;
            }

            var detach = action as Player.StateAction.Detach;
            if (detach != null) {
                var next = new CombatantsState(prev);
                foreach (var pawnId in attach.Pawns) {
                    CombatantState prevCombatant;
                    if (prev.TryGetValue(pawnId, out prevCombatant)) {
                        next[pawnId] = new CombatantState(prevCombatant) {
                            Player = PlayerId.Empty
                        };
                    }
                }
                return next;
            }

            var detachAll = action as Player.StateAction.DetachAll;
            if (detachAll != null) {
                var next = new CombatantsState(prev);
                foreach (var pair in prev) {
                    if (pair.Value.Player == detachAll.PlayerId) {
                        next[pair.Key] = new CombatantState(pair.Value) {
                            Player = PlayerId.Empty
                        };
                    }
                }
                return next;
            }

            var combatantAction = action as StateAction.CombatantAction;
            if (combatantAction != null) {
                CombatantState prevCombatant;
                if (prev.TryGetValue(combatantAction.PawnId, out prevCombatant)) {
                    return new CombatantsState(prev) {
                        [combatantAction.PawnId] = ReduceCombatant(prevCombatant, combatantAction)
                    };
                }
            }

            var playerAction = action as Player.StateAction.PlayerAction;
            if (playerAction != null) {
                var next = new CombatantsState(prev);
                foreach (var pair in prev) {
                    if (pair.Value.Player == playerAction.PlayerId) {
                        next[pair.Key] = ReducePlayerCombatant(pair.Value, playerAction);
                    }
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

            return prev;
        }

        public static CombatantState ReducePlayerCombatant(in CombatantState prev, in Player.StateAction.PlayerAction action) {
            var pushed = action as Player.StateAction.PushCommand;
            if (pushed != null) {
                return new CombatantState(prev) {
                    LastCommand = new Player.PushedCommand {
                        Command = pushed.Command,
                        When = pushed.When
                    }
                };
            }

            return prev;
        }
    }
}