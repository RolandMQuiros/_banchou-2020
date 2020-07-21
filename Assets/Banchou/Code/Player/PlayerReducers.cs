using System.Collections.Generic;
using Banchou.Pawn;

namespace Banchou.Player {
    public static partial class PlayerReducers {
        public static PlayersState ReducePlayers(in PlayersState prev, in object action) {
            var add = action as StateAction.AddPlayer;
            if (add != null && !prev.ContainsKey(add.PlayerId)) {
                return new PlayersState(prev) {
                    [add.PlayerId] = new PlayerState() {
                        Source = add.Source
                    }
                };
            }

            var remove = action as StateAction.RemovePlayer;
            if (remove != null) {
                var next = new PlayersState(prev);
                next.Remove(remove.PlayerId);
                return next;
            }

            var addPawn = action as Pawn.StateAction.AddPawn;
            if (addPawn != null) {
                PlayerState prevPlayer;
                if (prev.TryGetValue(addPawn.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        [addPawn.PlayerId] = ReducePlayer(prevPlayer, action)
                    };
                }
            }

            var removePawn = action as Pawn.StateAction.AddPawn;
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
            var attach = action as StateAction.AttachPlayerToPawn;
            if (attach != null) {
                return new PlayerState(prev) {
                    Pawn = attach.PawnId
                };
            }

            var detach = action as StateAction.DetachPlayerFromPawn;
            if (detach != null) {
                return new PlayerState(prev) {
                    Pawn = PawnId.Empty
                };
            }

            var addTarget = action as StateAction.AddPlayerTarget;
            if (addTarget != null && !prev.Targets.Contains(addTarget.Target)) {
                var targets = new HashSet<PawnId>(prev.Targets);
                targets.Add(addTarget.Target);

                return new PlayerState(prev) {
                    Targets = targets
                };
            }

            var removeTarget = action as StateAction.RemovePlayerTarget;
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