using System.Linq;

using Banchou.Player;

namespace Banchou.Pawn {
    public static class PawnsReducers {
        public static PawnsState Reduce(in PawnsState prev, in object action) {
            var add = action as StateAction.AddPawn;
            if (add != null) {
                PawnState pawn;
                if (!prev.TryGetValue(add.PawnId, out pawn)) {
                    return new PawnsState(prev) {
                        [add.PawnId] = new PawnState {
                            PlayerId = add.PlayerId,
                            PrefabKey = add.PrefabKey
                        }
                    };
                }
            }

            var remove = action as StateAction.RemovePawn;
            if (remove != null) {
                var next = new PawnsState(prev);
                next.Remove(remove.PawnId);
                return next;
            }

            var removePlayer = action as Player.StateAction.RemovePlayer;
            if (removePlayer != null) {
                var affected = prev.Values.Where(pawn => pawn.PlayerId == removePlayer.PlayerId);
                if (affected.Any()) {
                    return new PawnsState(
                        prev.Select(pair => (Id: pair.Key, Pawn: ReducePawn(pair.Value, removePlayer)))
                            .ToDictionary(pair => pair.Id, pair => pair.Pawn)
                    );
                }
            }

            var attach = action as Player.StateAction.AttachPlayerToPawn;
            if (attach != null) {
                PawnState prevPawn;
                if (prev.TryGetValue(attach.PawnId, out prevPawn)) {
                    return new PawnsState(prev) {
                        [attach.PawnId] = ReducePawn(prevPawn, action)
                    };
                }
            }

            var detach = action as Player.StateAction.DetachPlayerFromPawn;
            if (detach != null) {
                var next = new PawnsState(prev);
                foreach (var pair in prev) {
                    if (pair.Value.PlayerId == detach.PlayerId) {
                        next[pair.Key] = new PawnState(pair.Value) {
                            PlayerId = PlayerId.Empty
                        };
                    }
                }
                return next;
            }

            // On scene load, remove all Pawns without Players
            var sceneLoaded = action as Banchou.StateAction.SceneLoaded;
            if (sceneLoaded != null) {
                return new PawnsState(
                    prev.Where(pair => pair.Value.PlayerId == PlayerId.Empty)
                        .ToDictionary(pair => pair.Key, pair => pair.Value)
                );
            }

            var pawnAction = action as StateAction.PawnAction;
            if (pawnAction != null) {
                PawnState prevPawn;
                if (prev.TryGetValue(pawnAction.PawnId, out prevPawn)) {
                    return new PawnsState(prev) {
                        [pawnAction.PawnId] = ReducePawn(prevPawn, pawnAction)
                    };
                }
            }

            return prev;
        }

        private static PawnState ReducePawn(in PawnState prev, in object action) {
            var attach = action as Player.StateAction.AttachPlayerToPawn;
            if (attach != null) {
                return new PawnState(prev) {
                    PlayerId = attach.PlayerId
                };
            }

            var removePlayer = action as Player.StateAction.RemovePlayer;
            if (removePlayer != null && prev.PlayerId == removePlayer.PlayerId) {
                return new PawnState(prev) {
                    PlayerId = PlayerId.Empty
                };
            }

            return prev;
        }
    }
}