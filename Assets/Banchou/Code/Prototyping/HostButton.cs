using System;

using Redux;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Board;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Prototype {
    public class HostButton : MonoBehaviour {
        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private BoardActions _boardActions;
        private NetworkActions _networkActions;
        private PlayerActions _playerActions;

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            BoardActions boardActions,
            PlayerActions playerActions
        ) {
            _observeState = observeState;
            _dispatch = dispatch;
            _boardActions = boardActions;
            _networkActions = networkActions;
            _playerActions = playerActions;
        }

        public void Host() {
            var playerId = PlayerId.Create();
            _dispatch(_playerActions.AddLocalPlayer(playerId));
            _dispatch(_boardActions.SetScene("TestingGrounds"));

            var pawnId = PawnId.Create();
            _dispatch(_boardActions.AddPawn(pawnId, "Isaac"));
            _dispatch(_boardActions.AddPawn("Dumpster"));

            _dispatch(_playerActions.Attach(playerId, pawnId));

            SceneManager.LoadScene("BanchouBoard");
        }
    }
}