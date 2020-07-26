namespace Banchou.Network {
    public enum Mode {
        Local,
        Server,
        Client
    }

    public class NetworkSettingsState {
        public Mode Mode = Mode.Local;

        public NetworkSettingsState() { }
        public NetworkSettingsState(in NetworkSettingsState prev) {
            Mode = prev.Mode;
        }
    }
}