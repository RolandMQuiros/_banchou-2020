﻿using System;
using System.Linq;

using Redux;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Board;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;
using Banchou.Stage;

using Random = UnityEngine.Random;

namespace Banchou.Prototype {
    public class HostButton : MonoBehaviour {
        public int MinPing { get; set; } = 0;
        public int MaxPing { get; set; } = 0;
        public bool RollbackEnabled { get; set; } = true;

        private IObservable<GameState> _observeState;
        private GetState _getState;
        private Dispatcher _dispatch;
        private StageActions _StageActions;
        private BoardActions _boardActions;
        private NetworkActions _networkActions;
        private PlayersActions _playerActions;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            StageActions StageActions,
            BoardActions boardActions,
            PlayersActions playerActions
        ) {
            _observeState = observeState;
            _getState = getState;
            _dispatch = dispatch;
            _StageActions = StageActions;
            _boardActions = boardActions;
            _networkActions = networkActions;
            _playerActions = playerActions;

            observeState
                .Select(state => state.GetClients())
                .DistinctUntilChanged()
                .Pairwise()
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Delay(TimeSpan.FromSeconds(2f))
                .CatchIgnoreLog()
                .Subscribe(clientId => {
                    var playerId = getState().NextPlayerId();
                    dispatch(playerActions.AddPlayer(playerId, "Local Player", $"Player {clientId}", clientId));

                    var pawnId = getState().NextPawnId();
                    dispatch(boardActions.AddPawn(pawnId, playerId, "Isaac", new Vector3(Random.Range(-5f, 5f), 3f, Random.Range(-5f, 5f))));
                });
        }

        public void ParseMinPing(string ping) {
            int parsed;
            if (int.TryParse(ping, out parsed)) {
                MinPing = parsed;
            }
        }

        public void ParseMaxPing(string ping) {
            int parsed;
            if (int.TryParse(ping, out parsed)) {
                MaxPing = parsed;
            }
        }

        public void Host() {
            SceneManager.LoadScene("BanchouBoard");
            _dispatch(_networkActions.SetMode(
                Mode.Server,
                simulateMinLatency: MinPing,
                simulateMaxLatency: MaxPing,
                enableRollback: RollbackEnabled,
                rollbackDetectionMaxThreshold: 0.5f
            ));

            var playerId = _getState().NextPlayerId();
            _dispatch(_playerActions.AddPlayer(playerId, "Local Player"));
            _dispatch(_StageActions.SetScene("TestingGrounds"));

            var pawnId = _getState().NextPawnId();
            _dispatch(_boardActions.AddPawn(pawnId, "Isaac", new Vector3(0f, 3f, 0f)));
            _dispatch(_boardActions.AddPawn("Dumpster", new Vector3(10f, 5f, 5f)));

            _dispatch(_playerActions.Attach(playerId, pawnId));
        }
    }
}