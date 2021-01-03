using System;

using Redux;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Network;
using Banchou.Player;
using Banchou.Board;

namespace Banchou.Prototype {
    public class ConnectButton : MonoBehaviour {
        public string IPAddress { get; set; }
        public int MinPing { get; set; } = 0;
        public int MaxPing { get; set; } = 0;
        public bool RollbackEnabled { get; set; } = true;

        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private BoardActions _boardActions = new BoardActions();
        private NetworkActions _networkActions;

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            BoardActions boardActions,
            PlayersActions playerActions
        ) {
            _observeState = observeState;
            _dispatch = dispatch;
            _boardActions = boardActions;
            _networkActions = networkActions;
        }

        public void ParseMinPing(string ping) {
            MinPing = int.Parse(ping);
        }

        public void ParseMaxPing(string ping) {
            MaxPing = int.Parse(ping);
        }

        public void Connect() {
            SceneManager.LoadScene("BanchouBoard");
            _dispatch(_networkActions.SetMode(
                Mode.Client,
                enableRollback: RollbackEnabled,
                simulateMinLatency: MinPing,
                simulateMaxLatency: MaxPing
            ));
        }
    }
}