using Banchou.Board;
using Banchou.Combatant;
using Banchou.Network;
using Banchou.Mob;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou {
    public class GameState {
        public NetworkState Network = new NetworkState();
        public BoardState Board = new BoardState();
        public PlayersState Players = new PlayersState();
        public PawnsState Pawns = new PawnsState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();
        public GameState() { }
        public GameState(in GameState prev) {
            Network = prev.Network;
            Board = prev.Board;
            Players = prev.Players;
            Pawns = prev.Pawns;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
        }
    }
}