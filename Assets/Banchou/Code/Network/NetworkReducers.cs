using System;
using System.Linq;

namespace Banchou.Network {
    public static class NetworkReducers {
        public static NetworkState Reduce(NetworkState prev, object action) {
            if (action is StateAction.SetNetworkMode setMode) {
                return new NetworkState {
                    Mode = setMode.Mode,
                    IP = setMode.IP != null ? setMode.IP : prev.IP,
                    Id = setMode.Mode == Mode.Server ? Guid.NewGuid() : Guid.Empty,
                    SimulateMinLatency = setMode.Mode != Mode.Local ? setMode.SimulateMinLatency : 0,
                    SimulateMaxLatency = setMode.Mode != Mode.Local ? setMode.SimulateMaxLatency : 0,
                    IsRollbackEnabled = setMode.Mode != Mode.Local && setMode.EnableRollback,
                    RollbackHistoryDuration = setMode.RollbackHistoryDuration,
                    RollbackDetectionMaxThreshold = setMode.RollbackDetectionMaxThreshold,
                    LastUpdated = setMode.When
                };
            }

            if (action is StateAction.SimulateLatency setPing && prev.Mode != Mode.Local) {
                return new NetworkState {
                    SimulateMinLatency = setPing.SimulateMinLatency,
                    SimulateMaxLatency = setPing.SimulateMaxLatency,
                    LastUpdated = setPing.When
                };
            }

            if (action is StateAction.ConnectedToServer toServer && prev.Mode == Mode.Client) {
                return new NetworkState(prev) {
                    Id = toServer.ClientNetworkId,
                    LastUpdated = toServer.When
                };
            }

            if (action is StateAction.ConnectedToClient toClient && prev.Mode == Mode.Server) {
                return new NetworkState(prev) {
                    Clients = prev.Clients.Append(toClient.ClientNetworkId),
                    LastUpdated = toClient.When
                };
            }

            return prev;
        }
    }
}