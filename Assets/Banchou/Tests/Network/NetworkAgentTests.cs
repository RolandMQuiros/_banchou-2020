using System.Net;
using NUnit.Framework;

using Redux;
using Redux.Reactive;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;

namespace Banchou.Tests
{
    [TestFixture]
    public class NetworkAgentTests
    {
        private Store<GameState> _serverStore;
        private NetworkServer _server;
        private Subject<Unit> _pollServer;

        private Store<GameState> _clientStore;
        private NetworkClient _client;
        private Subject<Unit> _pollClient;

        [SetUp]
        public void Setup() {
            _serverStore = new Store<GameState>(GameStateStore.Reducer, new GameState(), NetworkServer.Install<GameState>());
            _server = new NetworkServer(
                _serverStore.ObserveState(),
                _serverStore.Dispatch,
                new PlayersActions(),
                new PlayerInputStreams()
            );

            _clientStore = new Store<GameState>(GameStateStore.Reducer, new GameState());
            _client = new NetworkClient(
                _clientStore.Dispatch,
                new NetworkActions(),
                new PlayerInputStreams(),
                p => { }
            );

            _pollServer = new Subject<Unit>();
            _server.Start(_pollServer);

            _pollClient = new Subject<Unit>();
            _client.Start(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9050), _pollClient);
        }

        [Test]
        public void AddPawn() {
            _serverStore.Dispatch(new Board.StateAction.AddPawn {
                PawnId = new Pawn.PawnId(12345),
                PlayerId = PlayerId.Empty,
                PrefabKey = "NoPrefab",
                SpawnPosition = new Vector3(10f, 12f, 13f),
                SpawnRotation = Quaternion.identity
            });

            _pollServer.OnNext(new Unit());
            _pollClient.OnNext(new Unit());
            var clientState = _clientStore.GetState();

            Assert.IsNotEmpty(clientState.Pawns.States);
        }
    }
}
