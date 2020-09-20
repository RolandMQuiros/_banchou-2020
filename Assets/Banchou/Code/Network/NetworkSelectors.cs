using System;
using System.Net;

namespace Banchou.Network {
    public static class NetworkSelectors {
        public static Mode GetNetworkMode(this GameState state) {
            return state.Network.Mode;
        }

        public static Guid GetNetworkId(this GameState state) {
            return state.Network.Id;
        }

        public static IPEndPoint GetIP(this GameState state) {
            return state.Network.IP;
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