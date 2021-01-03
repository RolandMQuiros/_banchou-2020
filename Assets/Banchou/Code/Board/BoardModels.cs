using Banchou.Pawn;
using Banchou.Mob;
using Banchou.Network;
using Banchou.Combatant;

namespace Banchou.Board {
    public class BoardState {
        public PawnsState Pawns = new PawnsState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();

        public RollbackPhase RollbackPhase = RollbackPhase.Complete;

        public BoardState() { }
        public BoardState(in BoardState prev) {
            Pawns = prev.Pawns;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
            RollbackPhase = prev.RollbackPhase;
        }
    }
}