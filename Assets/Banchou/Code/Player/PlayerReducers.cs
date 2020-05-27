using System.Collections.Generic;
using Banchou.Pawn;

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
            var attach = action as StateAction.Attach;
            if (attach != null) {
                return new PlayerState(prev) {
                    Pawn = attach.PawnId
                };
            }

            var detach = action as StateAction.Detach;
            if (detach != null) {
                return new PlayerState(prev) {
                    Pawn = PawnId.Empty
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

            var addTarget = action as StateAction.AddTarget;
            if (addTarget != null && !prev.Targets.Contains(addTarget.Target)) {
                var targets = new HashSet<PawnId>(prev.Targets);
                targets.Add(addTarget.Target);

                return new PlayerState(prev) {
                    Targets = targets
                };
            }

            var removeTarget = action as StateAction.RemoveTarget;
            if (removeTarget != null) {
                var target = removeTarget.Target;

                // Remove target from current targeting list
                var targets = prev.Targets;
                if (targets.Contains(target)) {
                    targets = new HashSet<PawnId>(prev.Targets);
                    targets.Remove(target);
                }

                // If any change is detected, update the PlayerState
                if (targets != prev.Targets) {
                    return new PlayerState(prev) {
                        Targets = targets
                    };
                }
            }

            return prev;
        }
    }
}