using MessagePack;

using Banchou.Board;
using Banchou.Combatant;
using Banchou.Network;
using Banchou.Mob;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou {
    [MessagePackObject]
    public class GameState {
        [Key(0)] public NetworkSettingsState Network = new NetworkSettingsState();
        [Key(1)] public BoardState Board = new BoardState();
        [Key(2)] public PlayersState Players = new PlayersState();
        [Key(3)] public PawnsState Pawns = new PawnsState();
        [Key(4)] public PawnSyncState PawnSync = new PawnSyncState();
        [Key(5)] public MobsState Mobs = new MobsState();
        [Key(6)] public CombatantsState Combatants = new CombatantsState();
        public GameState() { }
        public GameState(in GameState prev) {
            Network = prev.Network;
            Board = prev.Board;
            Players = prev.Players;
            Pawns = prev.Pawns;
            PawnSync = prev.PawnSync;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
        }
    }
}