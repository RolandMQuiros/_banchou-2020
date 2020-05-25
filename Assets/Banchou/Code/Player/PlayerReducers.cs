using System.Collections.Generic;
using System.Linq;

using Banchou.Pawn;
using Banchou.Player.Activation;
using Banchou.Player.Targeting;

namespace Banchou.Player {
    public static partial class PlayerReducers {
        public static PlayersState ReducePlayers(in PlayersState prev, in object action) {
            var add = action as StateAction.Add;
            if (add != null && !prev.ContainsKey(add.PlayerId)) {
                return new PlayersState(prev) {
                    [add.PlayerId] = new PlayerState() {
                        Source = add.Source
                    }
                };
            }

            var remove = action as StateAction.Remove;
            if (remove != null) {
                var next = new PlayersState(prev);
                next.Remove(remove.PlayerId);
                return next;
            }

            var addPawn = action as Pawn.StateAction.Add;
            if (addPawn != null) {
                PlayerState prevPlayer;
                if (prev.TryGetValue(addPawn.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        [addPawn.PlayerId] = ReducePlayer(prevPlayer, action)
                    };
                }
            }

            var removePawn = action as Pawn.StateAction.Add;
            if (removePawn != null) {
                PlayerState prevPlayer;
                if (prev.TryGetValue(removePawn.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        [removePawn.PlayerId] = ReducePlayer(prevPlayer, action)
                    };
                }
            }


            var playerAction = action as StateAction.PlayerAction;
            if (playerAction != null) {
                PlayerState prevPlayer;
                prev.TryGetValue(playerAction.PlayerId, out prevPlayer);
                return new PlayersState(prev) {
                    [playerAction.PlayerId] = ReducePlayer(prevPlayer, action)
                };
            }

            return prev;
        }

        private static PlayerState ReducePlayer(in PlayerState prev, in object action) {
            var addPawn = action as Pawn.StateAction.Add;
            if (addPawn != null) {
                var pawns = prev.Pawns.Append(addPawn.PawnId).ToList();
                var selected = prev.SelectedPawns;
                if (pawns.Count() == 1) {
                    selected = pawns;
                }

                return new PlayerState(prev) {
                    Pawns = pawns,
                    SelectedPawns = selected
                };
            }

            var removePawn = action as Pawn.StateAction.Remove;
            if (removePawn != null) {
                return RemovePawn(prev, removePawn.PawnId);
            }

            var attach = action as StateAction.Attach;
            if (attach != null) {
                return new PlayerState(prev) {
                    Pawns = prev.Pawns.Concat(attach.Pawns).ToList(),
                    SelectedPawns = !prev.Pawns.Any() ? attach.Pawns.Take(1).ToList() : prev.SelectedPawns
                };
            }

            var detach = action as StateAction.Detach;
            if (detach != null) {
                return RemovePawns(prev, detach.Pawns);
            }

            var detachAll = action as StateAction.DetachAll;
            if (detachAll != null) {
                return new PlayerState(prev) {
                    Pawns = Enumerable.Empty<PawnId>().ToList(),
                    SelectedPawns = new List<PawnId>()
                };
            }

            var pushCommand = action as StateAction.PushCommand;
            if (pushCommand != null) {
                return new PlayerState(prev) {
                    LastCommand = new PushedCommand {
                        Command = pushCommand.Command,
                        When = pushCommand.When
                    }
                };
            }

            var move = action as StateAction.Move;
            if (move != null && move.Direction != prev.InputMovement) {
                return new PlayerState(prev) {
                    InputMovement = move.Direction
                };
            }

            var look = action as StateAction.Look;
            if (look != null && look.Direction != prev.InputLook) {
                return new PlayerState(prev) {
                    InputLook = look.Direction
                };
            }

            return TargetingReducers.Reduce(prev, action) ?? ActivationReducers.Reduce(prev, action) ?? prev;
        }

        private static PlayerState RemovePawn(in PlayerState prev, params PawnId[] removed) {
            return RemovePawns(prev, removed);
        }

        private static PlayerState RemovePawns(in PlayerState prev, IEnumerable<PawnId> removed) {
            var hasPawn = prev.Pawns.Any(p => removed.Contains(p));
            var hasTarget = prev.Targets.Any(p => removed.Contains(p));
            var hasHistory = prev.Targets.Any(p => removed.Contains(p));
            var hasFuture = prev.Targets.Any(p => removed.Contains(p));
            var hasSelected = prev.SelectedPawns.Any(p => removed.Contains(p));
            var hasLockOn = removed.Contains(prev.LockOnTarget);

            if (hasPawn || hasTarget || hasSelected || hasLockOn) {
                return new PlayerState(prev) {
                    Pawns = hasPawn ? prev.Pawns.Except(removed).ToList() : prev.Pawns,
                    Targets = hasTarget ? new HashSet<PawnId>(prev.Targets.Except(removed)) : prev.Targets,
                    SelectedPawns = hasSelected ? prev.SelectedPawns.Except(removed).ToList() : prev.SelectedPawns,
                    LockOnHistory = hasHistory ? prev.LockOnHistory.Except(removed) : prev.LockOnHistory,
                    LockOnFuture = hasFuture ? prev.LockOnFuture.Except(removed) : prev.LockOnFuture,
                    LockOnTarget = hasLockOn ? PawnId.Empty : prev.LockOnTarget
                };
            }
            return prev;
        }
    }
}