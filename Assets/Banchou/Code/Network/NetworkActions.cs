using System;
using UnityEngine;
using Redux;

namespace Banchou.Network {
    namespace StateAction {
        public struct SetNetworkMode {
            public Mode Mode;
        }

        public struct NetworkAgentStarted {
            public int PeerId;
            public float When;
        }

        public struct ConnectedToServer {
            public Guid ClientNetworkId;
            public DateTime ServerTime;
        }

        public struct SyncGameState {
            public GameState GameState;
        }
    }

    public class NetworkActions {
        public StateAction.SetNetworkMode SetMode(Mode mode) => new StateAction.SetNetworkMode { Mode = mode };
        public StateAction.NetworkAgentStarted Started(int peerId) => new StateAction.NetworkAgentStarted { PeerId = peerId, When = Time.fixedUnscaledTime };
        public StateAction.ConnectedToServer ConnectedToServer(Guid clientNetworkid, DateTime serverTime) => new StateAction.ConnectedToServer { ClientNetworkId = clientNetworkid, ServerTime = serverTime };
        public StateAction.SyncGameState SyncGameState(GameState gameState) => new StateAction.SyncGameState { GameState = gameState };
    }
}