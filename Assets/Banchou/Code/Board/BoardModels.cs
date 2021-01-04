using Banchou.Pawn;
using Banchou.Mob;
using Banchou.Network;
using Banchou.Combatant;

namespace Banchou.Board {
    public class BoardState {
        public PawnsState Pawns = new PawnsState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();
        public float LastUpdated = 0f;

        public BoardState() { }
        public BoardState(in BoardState prev) {
            Pawns = prev.Pawns;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
            LastUpdated = prev.LastUpdated;
        }
    }
}