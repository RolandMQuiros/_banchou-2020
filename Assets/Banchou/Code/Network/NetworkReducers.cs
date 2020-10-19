
namespace Banchou.Network {
    public static class NetworkReducers {
        public static NetworkState Reduce(NetworkState prev, object action) {
            if (action is StateAction.SetNetworkMode setMode) {
                return new NetworkState {
                    Mode = setMode.Mode
                };
            }

            if (action is StateAction.ConnectedToServer connected) {
                return new NetworkState(prev) {
                    Id = connected.ClientNetworkId
                };
            }

            return prev;
        }
    }
}