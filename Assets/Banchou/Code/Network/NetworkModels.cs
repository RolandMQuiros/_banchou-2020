using System.Net;

namespace Banchou.Network {
    public enum Mode : byte {
        Local,
        Server,
        Client
    }

    public class NetworkSettingsState {
        public Mode Mode = Mode.Local;

        public bool IsConnecting = false;
        public IPEndPoint Host;

        public NetworkSettingsState() { }
        public NetworkSettingsState(in NetworkSettingsState prev) {
            Mode = prev.Mode;
            IsConnecting = prev.IsConnecting;
            Host = prev.Host;
        }
    }
}