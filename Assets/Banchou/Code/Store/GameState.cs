using Banchou.Player;
using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;

namespace Banchou {
    public class GameState {
        public PlayersState Players = new PlayersState();
        public PawnsState Pawns = new PawnsState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();
        public GameState() { }
        public GameState(in GameState prev) {
            Players = prev.Players;
            Pawns = prev.Pawns;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
        }
    }
}