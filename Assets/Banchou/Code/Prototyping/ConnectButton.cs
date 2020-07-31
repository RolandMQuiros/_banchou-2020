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

        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private BoardActions _boardActions = new BoardActions();
        private NetworkActions _networkActions = new NetworkActions();
        // private PlayerActions _playerActions = new PlayerActions();

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
            // _playerActions = playerActions;
        }

        public void Connect() {
            SceneManager.LoadScene("BanchouBoard");
            _dispatch(_networkActions.SetMode(Mode.Client));
        }
    }
}