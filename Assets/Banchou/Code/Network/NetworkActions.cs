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

        public struct ConnectedToClient {
            public Guid ClientNetworkId;
        }

        public struct SyncGameState {
            public GameState GameState;
        }
    }

    public class NetworkActions {
        private GetServerTime _getTime;
        public NetworkActions(GetServerTime getServerTime) {
            _getTime = getServerTime;
        }
        public StateAction.SetNetworkMode SetMode(Mode mode) => new StateAction.SetNetworkMode { Mode = mode };
        public StateAction.NetworkAgentStarted Started(int peerId) => new StateAction.NetworkAgentStarted { PeerId = peerId, When = _getTime() };
        public StateAction.ConnectedToServer ConnectedToServer(Guid clientNetworkid, DateTime serverTime) => new StateAction.ConnectedToServer { ClientNetworkId = clientNetworkid, ServerTime = serverTime };
        public StateAction.ConnectedToClient ConnectedToClient(Guid clientNetworkId) => new StateAction.ConnectedToClient { ClientNetworkId = clientNetworkId };
        public StateAction.SyncGameState SyncGameState(GameState gameState) => new StateAction.SyncGameState { GameState = gameState };
    }
}