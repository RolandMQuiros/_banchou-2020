using Banchou.Pawn;
using Banchou.Mob;
using Banchou.Combatant;

namespace Banchou.Board {
    public static class BoardReducers {
        public static BoardState Reduce(in BoardState prev, in object action) {
            if (action is Network.StateAction.SyncGameState sync) {
                return sync.GameState.Board;
            }

            if (action is StateAction.SyncBoard syncBoard) {
                return syncBoard.Board;
            }

            if (action is StateAction.RollbackBoard rollback) {
                return new BoardState {
                    RewindTime = rollback.Amount
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