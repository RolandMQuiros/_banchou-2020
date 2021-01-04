using System;

namespace Banchou.Network {
    namespace StateAction {
        public struct SetNetworkMode {
            public Mode Mode;
            public int SimulateMinLatency;
            public int SimulateMaxLatency;
            public bool EnableRollback;
            public float RollbackHistoryDuration;
            public float RollbackDetectionThreshold;
            public float When;
        }

        public struct SimulateLatency {
            public int SimulateMinLatency;
            public int SimulateMaxLatency;
            public float When;
        }

        public struct NetworkAgentStarted {
            public int PeerId;
            public float When;
        }

        public struct ConnectedToServer {
            public Guid ClientNetworkId;
            public DateTime ServerTime;
            public float When;
        }

        public struct ConnectedToClient {
            public Guid ClientNetworkId;
            public float When;
        }

        public struct SyncGameState {
            public GameState GameState;
        }
    }

    public class NetworkActions {
        private GetServerTime _getServerTime;
        public NetworkActions(GetServerTime getServerTime) {
            _getServerTime = getServerTime;
        }
        public StateAction.SetNetworkMode SetMode(
            Mode mode,
            bool enableRollback = true,
            float rollbackHistoryDuration = 0.5f,
            float rollbackDetectionThreshold = 0.17f,
            int simulateMinLatency = 0,
            int simulateMaxLatency = 0,
            float? when = null
        ) => new StateAction.SetNetworkMode {
            Mode = mode,
            EnableRollback = enableRollback,
            RollbackHistoryDuration = rollbackHistoryDuration,
            RollbackDetectionThreshold = rollbackDetectionThreshold,
            SimulateMinLatency = simulateMinLatency,
            SimulateMaxLatency = simulateMaxLatency,
            When = when ?? _getServerTime()
        };
        public StateAction.SimulateLatency SimulateLatency(int min, int max, float? when = null) => new StateAction.SimulateLatency {
            SimulateMinLatency = min,
            SimulateMaxLatency = max,
            When = when ?? _getServerTime()
        };
        public StateAction.NetworkAgentStarted Started(int peerId, float? when = null) => new StateAction.NetworkAgentStarted { PeerId = peerId, When = when ?? _getServerTime() };
        public StateAction.ConnectedToServer ConnectedToServer(Guid clientNetworkid, DateTime serverTime, float? when = null) => new StateAction.ConnectedToServer { ClientNetworkId = clientNetworkid, ServerTime = serverTime, When = when ?? _getServerTime() };
        public StateAction.ConnectedToClient ConnectedToClient(Guid clientNetworkId, float? when = null) => new StateAction.ConnectedToClient { ClientNetworkId = clientNetworkId, When = when ?? _getServerTime() };
        public StateAction.SyncGameState SyncGameState(GameState gameState) => new StateAction.SyncGameState { GameState = gameState };
    }
}