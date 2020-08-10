using System.Net;

namespace Banchou.Network {
    public enum Mode : byte {
        Local,
        Server,
        Client
    }

    public class NetworkState {
        public Mode Mode = Mode.Local;

        public bool IsConnecting = false;
        public IPEndPoint IP = new IPEndPoint(127, 9050);

        public NetworkState() { }
        public NetworkState(in NetworkState prev) {
            Mode = prev.Mode;
            IsConnecting = prev.IsConnecting;
            IP = prev.IP;
        }
    }
}