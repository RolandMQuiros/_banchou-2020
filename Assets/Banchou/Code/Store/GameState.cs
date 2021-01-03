﻿using Banchou.Board;
using Banchou.Combatant;
using Banchou.Network;
using Banchou.Mob;
using Banchou.Pawn;
using Banchou.Player;
using Banchou.Stage;

namespace Banchou {
    public class GameState {
        public NetworkState Network = new NetworkState();
        public StageState Stage = new StageState();
        public PlayersState Players = new PlayersState();
        public PawnsState Pawns = new PawnsState();
        public MobsState Mobs = new MobsState();
        public CombatantsState Combatants = new CombatantsState();
        public GameState() { }
        public GameState(in GameState prev) {
            Network = prev.Network;
            Stage = prev.Stage;
            Players = prev.Players;
            Pawns = prev.Pawns;
            Mobs = prev.Mobs;
            Combatants = prev.Combatants;
        }
    }
}