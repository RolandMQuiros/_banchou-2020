﻿using System;
using System.Linq;
using System.Collections.Generic;
using Banchou.Pawn;

using Banchou.Network;

namespace Banchou.Player {
    public static partial class PlayerReducers {
        public static PlayersState Reduce(in PlayersState prev, in NetworkState network, in object action) {
            if (action is StateAction.AddPlayer add && !prev.States.ContainsKey(add.PlayerId)) {
                return new PlayersState(prev) {
                    States = new Dictionary<PlayerId, PlayerState>(prev.States) {
                        [add.PlayerId] = new PlayerState() {
                            PrefabKey = add.PrefabKey,
                            NetworkId = add.NetworkId == Guid.Empty ? network.Id : add.NetworkId,
                            Name = add.Name
                        }
                    },
                    LastUpdated = add.When
                };
            }

            if (action is StateAction.RemovePlayer remove) {
                var next = new PlayersState(prev) {
                    States = new Dictionary<PlayerId, PlayerState>(prev.States),
                    LastUpdated = remove.When
                };
                next.States.Remove(remove.PlayerId);
                return next;
            }

            if (action is Board.StateAction.AddPawn addPawn) {
                PlayerState prevPlayer;
                if (prev.States.TryGetValue(addPawn.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        States = new Dictionary<PlayerId, PlayerState>(prev.States) {
                            [addPawn.PlayerId] = ReducePlayer(prevPlayer, action)
                        },
                        LastUpdated = addPawn.When
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
                if (prev.States.TryGetValue(playerAction.PlayerId, out prevPlayer)) {
                    return new PlayersState(prev) {
                        States = new Dictionary<PlayerId, PlayerState>(prev.States) {
                            [playerAction.PlayerId] = ReducePlayer(prevPlayer, action)
                        },
                        LastUpdated = playerAction.When
                    };
                }
            }

            if (action is Network.StateAction.SyncGameState sync) {
                var prevPlayers = prev.States;
                var localNetworkId = network.Id;
                return new PlayersState(prev) {
                    States = sync.GameState.GetPlayers()
                        .Select(pair => {
                            var syncedPlayerId = pair.Key;
                            var syncedPlayer = pair.Value;

                            PlayerState prevPlayer;
                            if (!prevPlayers.TryGetValue(syncedPlayerId, out prevPlayer)) {
                                return new KeyValuePair<PlayerId, PlayerState>(
                                    syncedPlayerId,
                                    new PlayerState(syncedPlayer) {
                                        PrefabKey = syncedPlayer.NetworkId == localNetworkId ? syncedPlayer.PrefabKey : null
                                    }
                                );
                            }

                            return new KeyValuePair<PlayerId, PlayerState>(
                                syncedPlayerId, prevPlayer
                            );
                        })
                        .ToDictionary(p => p.Key, p => p.Value),
                    LastUpdated = sync.GameState.Players.LastUpdated // is this what we want?
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

            return prev;
        }
    }
}