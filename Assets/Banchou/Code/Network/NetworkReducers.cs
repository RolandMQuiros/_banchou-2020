using System;
using System.Linq;

namespace Banchou.Network {
    public static class NetworkReducers {
        public static NetworkState Reduce(NetworkState prev, object action) {
            if (action is StateAction.SetNetworkMode setMode) {
                return new NetworkState {
                    Mode = setMode.Mode,
                    Id = setMode.Mode == Mode.Server ? Guid.NewGuid() : Guid.Empty,
                    SimulateMinLatency = setMode.Mode != Mode.Local ? setMode.SimulateMinLatency : 0,
                    SimulateMaxLatency = setMode.Mode != Mode.Local ? setMode.SimulateMaxLatency : 0
                };
            }

            if (action is StateAction.ConnectedToServer toServer && prev.Mode == Mode.Client) {
                return new NetworkState(prev) {
                    Id = toServer.ClientNetworkId
                };
            }

            if (action is StateAction.ConnectedToClient toClient && prev.Mode == Mode.Server) {
                return new NetworkState(prev) {
                    Clients = prev.Clients.Append(toClient.ClientNetworkId)
                };
            }

            return prev;
        }
    }
}