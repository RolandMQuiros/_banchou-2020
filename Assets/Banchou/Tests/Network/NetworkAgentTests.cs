using System.Linq;
using System.Threading;
using System.Net;
using NUnit.Framework;

using LiteNetLib;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Redux;
using Redux.Reactive;
using UniRx;
using UnityEngine;

using Banchou.Pawn;
using Banchou.Player;
using Banchou.Network;

namespace Banchou.Test {
    [TestFixture]
    public class NetworkAgentTests: INetLogger {
        private Store<GameState> _serverStore;
        private NetworkServer _server;
        private Subject<Unit> _pollServer;

        private Store<GameState> _clientStore;
        private NetworkClient _client;
        private Subject<Unit> _pollClient;

        [SetUp]
        public void Setup() {
            NetDebug.Logger = this;

            var settings = JsonConvert.DefaultSettings();
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters.Add(new Newtonsoft.Json.UnityConverters.Math.Vector3Converter());
            settings.Converters.Add(new Newtonsoft.Json.UnityConverters.Math.Vector2Converter());
            var serializer = JsonSerializer.Create(settings);

            var messagePackOptions = MessagePackSerializerOptions
                .Standard
                .WithResolver(CompositeResolver.Create(
                    StandardResolver.Instance,
                    BanchouMessagePackResolver.Instance
                ))
                .WithCompression(MessagePackCompression.Lz4BlockArray);

            _serverStore = new Store<GameState>(GameStateStore.Reducer, new GameState(), NetworkServer.Install<GameState>(serializer, messagePackOptions));
            _server = new NetworkServer(
                _serverStore.ObserveState(),
                _serverStore.Dispatch,
                new PlayersActions(),
                new PlayerInputStreams(),
                serializer,
                messagePackOptions
            );

            _clientStore = new Store<GameState>(GameStateStore.Reducer, new GameState());
            _client = new NetworkClient(
                _clientStore.Dispatch,
                new NetworkActions(),
                new PlayerInputStreams(),
                p => { },
                serializer,
                messagePackOptions
            );

            _pollServer = new Subject<Unit>();
            _server.Start(_pollServer);
            _pollServer.OnNext(new Unit());

            _pollClient = new Subject<Unit>();
            _client.Start(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 9050), _pollClient);
            _pollClient.OnNext(new Unit());
        }

        [TearDown]
        public void TearDown() {
            _server?.Dispose();
            _client?.Dispose();
        }

        [Test]
        public void AddPawn() {
            for (int i = 0; i < 100; i++) {
                _pollServer.OnNext(new Unit());
                _pollClient.OnNext(new Unit());
                Thread.Sleep(1);
            }

            _serverStore.Dispatch(new Board.StateAction.AddPawn {
                PawnId = new PawnId(12345),
                PlayerId = PlayerId.Empty,
                PrefabKey = "NoPrefab",
                SpawnPosition = new Vector3(10f, 12f, 13f),
                SpawnRotation = Quaternion.identity
            });

            for (int i = 0; i < 100; i++) {
                _pollServer.OnNext(new Unit());
                _pollClient.OnNext(new Unit());
                Thread.Sleep(1);
            }

            var clientState = _clientStore.GetState();

            Assert.IsNotEmpty(clientState.Pawns.States);
            Assert.AreEqual(clientState.Pawns.States[new PawnId(12345)].PlayerId, PlayerId.Empty);
            Assert.AreEqual(clientState.Pawns.States[new PawnId(12345)].PrefabKey, "NoPrefab");
            Assert.AreEqual(clientState.Pawns.States[new PawnId(12345)].SpawnPosition, new Vector3(10f, 12f, 13f));
            Assert.AreEqual(clientState.Pawns.States[new PawnId(12345)].SpawnRotation, Quaternion.identity);

            Thread.Sleep(1000);
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args) {
            Debug.LogFormat($"<color=#42f5d1>{level}</color>{str}", args);
        }
    }
}
