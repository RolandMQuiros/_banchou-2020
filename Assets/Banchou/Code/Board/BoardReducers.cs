using Banchou.Combatant;
using Banchou.Pawn;
using Banchou.Mob;
using Banchou.Network;

namespace Banchou.Board {
    public static class BoardReducers {
        public static BoardState Reduce(in BoardState prev, in NetworkState network, in object action) {
            if (action is Network.StateAction.SyncGameState sync) {
                return sync.GameState.Board;
            }

            if (action is StateAction.SyncBoard syncBoard) {
                return syncBoard.Board;
            }

            if (action is StateAction.RollbackBoard rollback && network.IsRollbackEnabled) {
                return new BoardState(prev) {
                    Pawns = rollback.Board.Pawns,
                    Mobs = rollback.Board.Mobs,
                    Combatants = rollback.Board.Combatants
                };
            }

            if (action is StateAction.ResimulateBoard && network.IsRollbackEnabled) {
                return new BoardState(prev) {
                    RollbackPhase = RollbackPhase.Resimulate
                };
            }

            if (action is StateAction.CompleteBoardRollback && network.IsRollbackEnabled) {
                return new BoardState(prev) {
                    RollbackPhase = RollbackPhase.Complete
                };
            }

            var next = new BoardState(prev);
            var didChange =
                prev.Pawns != (next.Pawns = PawnsReducers.Reduce(prev.Pawns, action)) ||
                prev.Mobs != (next.Mobs = MobsReducers.Reduce(prev.Mobs, action)) ||
                prev.Combatants != (next.Combatants = CombatantsReducers.Reduce(prev.Combatants, action));

            return didChange ? next : prev;
        }
    }
}