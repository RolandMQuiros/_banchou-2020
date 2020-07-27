﻿using System.Collections.Generic;
using System.Linq;

using Banchou.Player;

namespace Banchou.Pawn {
    public static class PawnsReducers {
        public static PawnsState Reduce(in PawnsState prev, in object action) {
            if (action is Board.StateAction.AddPawn add) {
                PawnState pawn;
                if (!prev.States.TryGetValue(add.PawnId, out pawn)) {
                    return new PawnsState(prev) {
                        States = new Dictionary<PawnId, PawnState>(prev.States) {
                            [add.PawnId] = new PawnState {
                                PlayerId = add.PlayerId,
                                PrefabKey = add.PrefabKey
                            }
                        }
                    };
                }
            }

            if (action is Board.StateAction.RemovePawn remove) {
                var next = new PawnsState(prev);
                next.States.Remove(remove.PawnId);
                return next;
            }

            if (action is Player.StateAction.RemovePlayer removePlayer) {
                var affected = prev.States.Values.Where(pawn => pawn.PlayerId == removePlayer.PlayerId);
                if (affected.Any()) {
                    return new PawnsState(prev) {
                        States = prev.States.Select(pair => (Id: pair.Key, Pawn: ReducePawn(pair.Value, removePlayer)))
                            .ToDictionary(pair => pair.Id, pair => pair.Pawn)
                    };
                }
            }

            if (action is Player.StateAction.AttachPlayerToPawn attach) {
                PawnState prevPawn;
                if (prev.States.TryGetValue(attach.PawnId, out prevPawn)) {
                    return new PawnsState(prev) {
                        States = new Dictionary<PawnId, PawnState>(prev.States) {
                            [attach.PawnId] = ReducePawn(prevPawn, action)
                        }
                    };
                }
            }

            if (action is Player.StateAction.DetachPlayerFromPawn detach) {
                var next = new Dictionary<PawnId, PawnState>(prev.States);
                foreach (var pair in prev.States) {
                    if (pair.Value.PlayerId == detach.PlayerId) {
                        next[pair.Key] = new PawnState(pair.Value) {
                            PlayerId = PlayerId.Empty
                        };
                    }
                }
                return new PawnsState(prev) {
                    States = next
                };
            }

            // On scene load, remove all Pawns without Players
            if (action is Banchou.StateAction.SceneLoaded) {
                return new PawnsState(prev) {
                    States = prev.States.Where(pair => pair.Value.PlayerId == PlayerId.Empty)
                        .ToDictionary(pair => pair.Key, pair => pair.Value)
                };
            }

            if (action is StateAction.FSMStateChanged fsmStateChanged) {
                return new PawnsState(prev) {
                    LatestFSMChange = new PawnFSMState(prev.LatestFSMChange) {
                        PawnId = fsmStateChanged.PawnId,
                        StateHash = fsmStateChanged.Statehash,
                        IsLoop = fsmStateChanged.IsLoop,
                        ClipLength = fsmStateChanged.ClipLength,
                        FixedTimeAtChange = fsmStateChanged.When
                    }
                };
            }

            if (action is StateAction.IPawnAction pawnAction) {
                PawnState prevPawn;
                if (prev.States.TryGetValue(pawnAction.PawnId, out prevPawn)) {
                    return new PawnsState(prev) {
                        States = new Dictionary<PawnId, PawnState>(prev.States) {
                            [pawnAction.PawnId] = ReducePawn(prevPawn, pawnAction)
                        }
                    };
                }
            }

            return prev;
        }

        private static PawnState ReducePawn(in PawnState prev, in object action) {
            if (action is Player.StateAction.AttachPlayerToPawn attach) {
                return new PawnState(prev) {
                    PlayerId = attach.PlayerId
                };
            }

            if (action is Player.StateAction.RemovePlayer removePlayer && prev.PlayerId == removePlayer.PlayerId) {
                return new PawnState(prev) {
                    PlayerId = PlayerId.Empty
                };
            }

            if (action is StateAction.RollbackStarted rollbackStarted) {
                return new PawnState(prev) {
                    RollbackState = PawnRollbackState.RollingBack
                };
            }

            if (action is StateAction.FastForwarding fastForwarding) {
                return new PawnState(prev) {
                    RollbackState = PawnRollbackState.FastForward,
                    RollbackCorrectionTime = fastForwarding.CorrectionTime
                };
            }

            if (action is StateAction.RollbackComplete rollbackComplete) {
                return new PawnState(prev) {
                    RollbackState = PawnRollbackState.Complete
                };
            }

            return prev;
        }
    }

    public static class PawnSyncReducers {
        public static PawnSyncState Reduce(in PawnSyncState prev, in object action) {
            if (action is StateAction.SyncPawn syncPawn) {
                return new PawnSyncState {
                    PawnId = syncPawn.PawnId,
                    Position = syncPawn.Position,
                    Rotation = syncPawn.Rotation
                };
            }

            return prev;
        }
    }
}