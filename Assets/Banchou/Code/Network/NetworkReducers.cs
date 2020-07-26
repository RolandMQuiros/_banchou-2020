namespace Banchou.Network {
    public static class NetworkReducers {
        public static NetworkSettingsState Reduce(NetworkSettingsState prev, object action) {
            if (action is StateAction.SetNetworkMode setMode) {
                return new NetworkSettingsState {
                    Mode = setMode.Mode
                };
            }

            return prev;
        }
    }
}