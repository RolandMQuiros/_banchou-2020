using System.Net;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using Redux;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using Banchou.DependencyInjection;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Test {
    [TestFixture]
    public class RollbackTests : SceneTest {
        private const string TestScene = "Assets/Banchou/Tests/PlayMode/Network/ServerAndClientTests.unity";
        protected override IEnumerable<string> ScenePaths { get; } = new [] { TestScene };

        private Scene _scene;
        private GameObject _serverRoot;
        private GameObject _clientRoot;

        private class BoardMembers {
            public GetState GetState;
            public Dispatcher Dispatch;
            public GetTime GetTime;

            public PlayerInputStreams Input;
            public IPawnInstances Pawns;

            public void Construct(
                GetState getState,
                Dispatcher dispatch,
                GetTime getTime,
                PlayerInputStreams playerInput,
                IPawnInstances pawns
            ) {
                GetState = getState;
                Dispatch = dispatch;
                GetTime = getTime;
                Input = playerInput;
                Pawns = pawns;
            }
        }

        private BoardMembers _serverBoard;
        private BoardMembers _clientBoard;


        [SetUp]
        public void SetUp() {
            LoadScene(
                TestScene,
                LoadSceneMode.Additive,
                (scene, _) => {
                    _scene = scene;
                    var roots = _scene.GetRootGameObjects();
                    _serverRoot = roots.First(obj => obj.name == "Server");
                    _clientRoot = roots.First(obj => obj.name == "Client");

                    _serverBoard = _serverRoot.transform
                        .FindContexts()
                        .ToDiContainer()
                        .Inject(new BoardMembers());
                    _clientBoard = _clientRoot.transform
                        .FindContexts()
                        .ToDiContainer()
                        .Inject(new BoardMembers());
                }
            );
        }

        private IEnumerator SetupServerAndClient(int minPing = 0, int maxPing = 0) {
            _serverBoard.Dispatch(new Network.StateAction.SetNetworkMode {
                Mode = Network.Mode.Server,
                SimulateMinLatency = minPing,
                SimulateMaxLatency = maxPing
            });

            _clientBoard.Dispatch(new Network.StateAction.SetNetworkMode {
                IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050),
                Mode = Network.Mode.Client,
                SimulateMinLatency = minPing,
                SimulateMaxLatency = maxPing
            });

            Assert.That(_serverBoard.GetState().GetNetworkMode() == Network.Mode.Server, "Server's network mode was not correctly set");
            Assert.That(_clientBoard.GetState().GetNetworkMode() == Network.Mode.Client, "Client's network mode was not correctly set");

            yield return new WaitUntil(() => _clientBoard.GetState().IsConnectedToServer());
        }

        [UnityTest]
        public IEnumerator DidLoadScene() {
            Assert.IsNotNull(_serverRoot, "No \"Server\" board found");
            Assert.IsNotNull(_clientRoot, "No \"Client\" board found");
            yield break;
        }

        [UnityTest]
        public IEnumerator ServerCreatesPawnsOnClient() {
            yield return SetupServerAndClient();

            var pawnId = new Pawn.PawnId(12345);
            _serverBoard.Dispatch(new Board.StateAction.AddPawn {
                PawnId = pawnId,
                PrefabKey = "NetworkedPawn"
            });

            yield return new WaitForSecondsRealtime(1f);

            Assert.That(_clientBoard.GetState().HasPawn(pawnId), "Client did not receive AddPawn action");
        }

        [UnityTest]
        public IEnumerator ServerAndClientTimeMatch() {
            yield return SetupServerAndClient(0, 0);

            var times = new List<(float Server, float Client)>();
            for (int i = 0; i <= 10; i++) {
                var pair = (Server: _serverBoard.GetTime(), Client: _clientBoard.GetTime());
                times.Add(pair);
                Debug.Log($"Server: {pair.Server}, Client: {pair.Client}, Diff: {pair.Server - pair.Client}");
                yield return new WaitForSecondsRealtime(1f);
            }

            Assert.That(!times.Any(t => Mathf.Abs(t.Server - t.Client) > Time.fixedUnscaledDeltaTime), $"Server and client have mismatched timestamps");
        }

        [UnityTest]
        public IEnumerator ServerInputCommand() {
            yield return SetupServerAndClient(300, 300);

            var playerId = new Player.PlayerId(1);
            _serverBoard.Dispatch(new Player.StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = "Local Player",
                Name = "Server Player"
            });

            var pawnId = new Pawn.PawnId(1);
            _serverBoard.Dispatch(new Board.StateAction.AddPawn {
                PawnId = pawnId,
                PrefabKey = "Isaac",
                PlayerId = playerId,
                SpawnPosition = new Vector3(0f, 1f, 0f)
            });

            yield return new WaitUntil(() => _clientBoard.GetState().HasPawn(pawnId));

            Assert.That(_clientBoard.GetState().HasPawn(pawnId), "Client did not receive AddPawn action");
            Assert.NotNull(_clientBoard.Pawns.Get(pawnId), "Client does not have an instance for Server-instantiated Pawn");
            Assert.AreEqual(_clientBoard.GetState().GetPawnPlayerId(pawnId), playerId, "Client's copy of the pawn is not assigned to the correct Player");

            Assert.AreEqual(
                _serverBoard.Pawns.Get(pawnId).Position,
                _clientBoard.Pawns.Get(pawnId).Position,
                "Client pawn instance not spawned in the same location"
            );

            _serverBoard.Input.PushMove(playerId, Vector3.forward, _serverBoard.GetTime());
            yield return new WaitForSecondsRealtime(1f);
            _serverBoard.Input.PushMove(playerId, Vector3.zero, _serverBoard.GetTime());
            yield return new WaitForSecondsRealtime(1f);

            Assert.AreEqual(
                _serverBoard.Pawns.Get(pawnId).Position,
                _clientBoard.Pawns.Get(pawnId).Position,
                "Client pawn instance not in same location after movement"
            );
        }
    }
}
