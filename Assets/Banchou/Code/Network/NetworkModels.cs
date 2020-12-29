using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Banchou.Network {
    public enum Mode : byte {
        Local,
        Server,
        Client
    }

    public class NetworkState {
        public Guid Id = Guid.Empty;
        public Mode Mode = Mode.Local;
        public int PeerId = 0;
        public bool IsConnecting = false;
        public IPEndPoint IP = new IPEndPoint(127, 9050);
        public IEnumerable<Guid> Clients = Enumerable.Empty<Guid>();

        public NetworkState() { }
        public NetworkState(in NetworkState prev) {
            Id = prev.Id;
            Mode = prev.Mode;
            PeerId = prev.PeerId;
            IsConnecting = prev.IsConnecting;
            IP = prev.IP;
            Clients = prev.Clients;
        }
    }

    public delegate float GetServerTime();
}