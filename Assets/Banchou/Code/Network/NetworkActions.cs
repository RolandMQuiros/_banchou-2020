using MessagePack;

namespace Banchou.Network {
    namespace StateAction {
        [MessagePackObject]
        public struct SetNetworkMode {
            [Key(0)] public Mode Mode;
        }

        [MessagePackObject]
        public struct SyncGameState {
            [Key(0)] public GameState GameState;
        }
    }

    public class NetworkActions {
        public StateAction.SetNetworkMode SetMode(Mode mode) => new StateAction.SetNetworkMode { Mode = mode };
        public StateAction.SyncGameState SyncGameState(GameState gameState) => new StateAction.SyncGameState { GameState = gameState };
    }
}