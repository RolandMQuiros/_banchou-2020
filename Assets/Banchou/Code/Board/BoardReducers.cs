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
                return new BoardState(syncBoard.Board) {
                    LastUpdated = syncBoard.When
                };
            }

            if (action is StateAction.RollbackBoard rollback && network.IsRollbackEnabled) {
                return rollback.Board;
            }

            var next = new BoardState(prev);
            var didChange =
                prev.Pawns != (next.Pawns = PawnsReducers.Reduce(prev.Pawns, network, action)) ||
                prev.Mobs != (next.Mobs = MobsReducers.Reduce(prev.Mobs, action)) ||
                prev.Combatants != (next.Combatants = CombatantsReducers.Reduce(prev.Combatants, action));
            if (didChange) {
                if (prev.Pawns != next.Pawns) {
                    next.LastUpdated = next.Pawns.LastUpdated;
                } else if (prev.Mobs != next.Mobs) {
                    next.LastUpdated = next.Mobs.LastUpdated;
                } else if (prev.Combatants != next.Combatants) {
                    next.LastUpdated = next.Combatants.LastUpdated;
                }
            }

            return didChange ? next : prev;
        }
    }
}