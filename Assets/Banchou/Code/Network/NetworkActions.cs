using System;
using System.Net;

namespace Banchou.Network {
    namespace StateAction {
        public struct SetNetworkMode {
            public Mode Mode;
            public IPEndPoint IP;
            public int SimulateMinLatency;
            public int SimulateMaxLatency;
            public bool EnableRollback;
            public bool EnablePhysicsRollback;
            public float RollbackHistoryDuration;
            public float RollbackDetectionMinThreshold;
            public float RollbackDetectionMaxThreshold;
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

        [LocalAction]
        public struct ConnectedToClient {
            public Guid ClientNetworkId;
            public float When;
        }

        public struct SyncGameState {
            public GameState GameState;
        }
    }

    public class NetworkActions {
        private GetTime _getTime;
        public NetworkActions(GetTime getTime) {
            _getTime = getTime;
        }
        public StateAction.SetNetworkMode SetMode(
            Mode mode,
            IPEndPoint ip = null,
            int simulateMinLatency = 0,
            int simulateMaxLatency = 0,
            bool enableRollback = true,
            bool enablePhysicsRollback = true,
            float rollbackHistoryDuration = 0.5f,
            float rollbackDetectionMinThreshold = 0.017f,
            float rollbackDetectionMaxThreshold = 0.17f,
            float? when = null
        ) => new StateAction.SetNetworkMode {
            Mode = mode,
            IP = ip,
            SimulateMinLatency = simulateMinLatency,
            SimulateMaxLatency = simulateMaxLatency,
            EnableRollback = enableRollback,
            EnablePhysicsRollback = enablePhysicsRollback,
            RollbackHistoryDuration = rollbackHistoryDuration,
            RollbackDetectionMinThreshold = rollbackDetectionMinThreshold,
            RollbackDetectionMaxThreshold = rollbackDetectionMaxThreshold,
            When = when ?? _getTime()
        };
        public StateAction.SimulateLatency SimulateLatency(int min, int max, float? when = null) => new StateAction.SimulateLatency {
            SimulateMinLatency = min,
            SimulateMaxLatency = max,
            When = when ?? _getTime()
        };
        public StateAction.NetworkAgentStarted Started(int peerId, float? when = null) => new StateAction.NetworkAgentStarted { PeerId = peerId, When = when ?? _getTime() };
        public StateAction.ConnectedToServer ConnectedToServer(Guid clientNetworkid, DateTime serverTime, float? when = null) => new StateAction.ConnectedToServer { ClientNetworkId = clientNetworkid, ServerTime = serverTime, When = when ?? _getTime() };
        public StateAction.ConnectedToClient ConnectedToClient(Guid clientNetworkId, float? when = null) => new StateAction.ConnectedToClient { ClientNetworkId = clientNetworkId, When = when ?? _getTime() };
        public StateAction.SyncGameState SyncGameState(GameState gameState) => new StateAction.SyncGameState { GameState = gameState };
    }
}