using Banchou.Board;
using Banchou.Network;
using Banchou.Player;
using Banchou.Stage;

namespace Banchou {
    public class GameState {
        public NetworkState Network = new NetworkState();
        public StageState Stage = new StageState();
        public PlayersState Players = new PlayersState();
        public BoardState Board = new BoardState();

        public GameState() { }
        public GameState(in GameState prev) {
            Network = prev.Network;
            Stage = prev.Stage;
            Players = prev.Players;
            Board = prev.Board;
        }
    }
}