using System;

using Redux;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Board;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Prototype {
    public class BatchModeStartup : MonoBehaviour {
        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private BoardActions _boardActions;
        private NetworkActions _networkActions;
        private PlayersActions _playerActions;

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
            _playerActions = playerActions;
        }

        private void Start() {
            if (Application.isBatchMode) {
                SceneManager.LoadScene("BanchouBoard");
                _dispatch(_networkActions.SetMode(Mode.Server));

                var playerId = PlayerId.Create();
                _dispatch(_playerActions.AddLocalPlayer(playerId));
                _dispatch(_boardActions.SetScene("TestingGrounds"));

                var pawnId = PawnId.Create();
                _dispatch(_boardActions.AddPawn(pawnId, "Isaac", new Vector3(0f, 3f, 0f)));
                _dispatch(_boardActions.AddPawn("Dumpster", new Vector3(10f, 3f, 5f)));

                _dispatch(_playerActions.Attach(playerId, pawnId));
            }
        }
    }
}