using System.Collections.Generic;
using Banchou.Pawn;

namespace Banchou.Player {
    public static partial class PlayerReducers {
        public static PlayersState Reduce(in PlayersState prev, in object action) {
            if (action is StateAction.AddPlayer add && !prev.ContainsKey(add.PlayerId)) {
                NetworkInfo netInfo = null;
                if (add.Source == InputSource.Network) {
                    netInfo = new NetworkInfo {
                        IP = add.IP,
                        PeerId = add.PeerId
                    };
                }

                return new PlayersState(prev) {
                    [add.PlayerId] = new PlayerState() {
                        Source = add.Source,
                        NetworkInfo = netInfo
                    }
                };
            }

            if (action is StateAction.RemovePlayer remove) {
                var next = new PlayersState(prev);
                next.Remove(remove.PlayerId);
                return next;
            }

            if (action is Pawn.StateAction.AddPawn addPawn) {
                PlayerState prevPlayer;
                if (prev.TryGetValue(addPawn.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        [addPawn.PlayerId] = ReducePlayer(prevPlayer, action)
                    };
                }
            }

            // if (action is Pawn.StateAction.RemovePawn removePawn) {
            //     PlayerState prevPlayer;
            //     if (prev.TryGetValue(removePawn.PlayerId, out prevPlayer)) {
            //         return new PlayersState(prev) {
            //             [removePawn.PlayerId] = ReducePlayer(prevPlayer, action)
            //         };
            //     }
            // }

            if (action is StateAction.IPlayerAction playerAction) {
                PlayerState prevPlayer;
                prev.TryGetValue(playerAction.PlayerId, out prevPlayer);
                return new PlayersState(prev) {
                    [playerAction.PlayerId] = ReducePlayer(prevPlayer, action)
                };
            }

            return prev;
        }

        private static PlayerState ReducePlayer(in PlayerState prev, in object action) {
            if (action is StateAction.AttachPlayerToPawn attach) {
                return new PlayerState(prev) {
                    Pawn = attach.PawnId
                };
            }

            if (action is StateAction.DetachPlayerFromPawn detach) {
                return new PlayerState(prev) {
                    Pawn = PawnId.Empty
                };
            }

            if (action is StateAction.AddPlayerTarget addTarget && !prev.Targets.Contains(addTarget.Target)) {
                var targets = new HashSet<PawnId>(prev.Targets);
                targets.Add(addTarget.Target);

                return new PlayerState(prev) {
                    Targets = targets
                };
            }

            if (action is StateAction.RemovePlayerTarget removeTarget) {
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