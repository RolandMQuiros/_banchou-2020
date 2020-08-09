using MessagePack;
using System.Net;

namespace Banchou.Network {
    public enum Mode : byte {
        Local,
        Server,
        Client
    }

    [MessagePackObject]
    public class NetworkSettingsState {
        [Key(0)] public Mode Mode = Mode.Local;

        [Key(1)] public bool IsConnecting = false;
        [Key(2)] public string Host;

        public NetworkSettingsState() { }
        public NetworkSettingsState(in NetworkSettingsState prev) {
            Mode = prev.Mode;
            IsConnecting = prev.IsConnecting;
            Host = prev.Host;
        }
    }
}