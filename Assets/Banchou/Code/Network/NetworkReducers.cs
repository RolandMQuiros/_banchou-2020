namespace Banchou.Network {
    public static class NetworkReducers {
        public static NetworkState Reduce(NetworkState prev, object action) {
            if (action is StateAction.SetNetworkMode setMode) {
                return new NetworkState {
                    Mode = setMode.Mode
                };
            }

            return prev;
        }
    }
}