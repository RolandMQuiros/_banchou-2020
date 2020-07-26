namespace Banchou.Network {
    public static class NetworkSelectors {
        public static Mode GetNetworkMode(this GameState state) {
            return state.Network.Mode;
        }

        public static bool IsLocal(this GameState state) {
            return state.Network.Mode == Mode.Local;
        }

        public static bool IsServer(this GameState state) {
            return state.Network.Mode == Mode.Server;
        }

        public static bool IsClient(this GameState state) {
            return state.Network.Mode == Mode.Client;
        }
    }
}