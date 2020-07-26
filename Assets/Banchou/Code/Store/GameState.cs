using Banchou.Network;
using Banchou.Player;
using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;

namespace Banchou {
    public class GameState {
        public NetworkSettingsState Network = new NetworkSettingsState();
        public PlayersState Players = new PlayersState();
        public PawnsState Pawns = new PawnsState();
        public PawnSyncState PawnSync = new PawnSyncState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();
        public GameState() { }
        public GameState(in GameState prev) {
            Network = prev.Network;
            Players = prev.Players;
            Pawns = prev.Pawns;
            PawnSync = prev.PawnSync;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
        }
    }
}