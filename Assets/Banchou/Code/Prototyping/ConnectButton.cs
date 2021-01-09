using System;
using System.Net;

using Redux;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Network;
using Banchou.Player;

namespace Banchou.Prototype {
    public class ConnectButton : MonoBehaviour {
        public bool RollbackEnabled { get; set; } = true;
        private IPEndPoint _ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);
        public int _minPing = 0;
        public int _maxPing = 0;

        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private NetworkActions _networkActions;

        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            NetworkActions networkActions,
            PlayersActions playerActions
        ) {
            _observeState = observeState;
            _dispatch = dispatch;
            _networkActions = networkActions;
        }

        public void ParseIP(string ip) {
            IPAddress parsed;
            if (IPAddress.TryParse(ip, out parsed)) {
                _ip.Address = parsed;
            }
        }

        public void ParsePort(string port) {
            int parsed;
            if (int.TryParse(port, out parsed)) {
                _ip.Port = parsed;
            }
        }

        public void ParseMinPing(string ping) {
            int parsed;
            if (int.TryParse(ping, out parsed)) {
                _minPing = parsed;
            }
        }

        public void ParseMaxPing(string ping) {
            int parsed;
            if (int.TryParse(ping, out parsed)) {
                _maxPing = parsed;
            }
        }

        public void Connect() {
            SceneManager.LoadScene("BanchouBoard");

            _dispatch(_networkActions.SetMode(
                Mode.Client,
                ip: _ip,
                enableRollback: RollbackEnabled,
                simulateMinLatency: _minPing,
                simulateMaxLatency: _maxPing
            ));
        }
    }
}