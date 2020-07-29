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

            if (action is Board.StateAction.AddPawn addPawn) {
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
                if (prev.TryGetValue(playerAction.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        [playerAction.PlayerId] = ReducePlayer(prevPlayer, action)
                    };
                }
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

            return prev;
        }
    }
}