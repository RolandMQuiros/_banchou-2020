using System;

using Redux;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Board;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Prototype {
    public class BatchModeStartup : MonoBehaviour {
        private IObservable<GameState> _observeState;
        private GetState _getState;
        private Dispatcher _dispatch;
        private BoardActions _boardActions;
        private NetworkActions _networkActions;
        private PlayersActions _playerActions;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            BoardActions boardActions,
            PlayersActions playerActions
        ) {
            _observeState = observeState;
            _getState = getState;
            _dispatch = dispatch;
            _boardActions = boardActions;
            _networkActions = networkActions;
            _playerActions = playerActions;
        }

        private void Start() {
            if (Application.isBatchMode) {
                SceneManager.LoadScene("BanchouBoard");
                _dispatch(_networkActions.SetMode(Mode.Server));

                var playerId = _getState().NextPlayerId();
                _dispatch(_playerActions.AddPlayer(playerId, "Local Player"));
                _dispatch(_boardActions.SetScene("TestingGrounds"));

                var pawnId = _getState().NextPawnId();
                _dispatch(_boardActions.AddPawn(pawnId, "Isaac", new Vector3(0f, 3f, 0f)));
                _dispatch(_boardActions.AddPawn("Dumpster", new Vector3(10f, 3f, 5f)));

                _dispatch(_playerActions.Attach(playerId, pawnId));
            }
        }
    }
}