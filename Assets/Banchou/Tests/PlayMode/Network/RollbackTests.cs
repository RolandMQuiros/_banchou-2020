using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Test {
    [TestFixture]
    public class RollbackTests : SceneTest {
        private const string TestScene = "Assets/Banchou/Tests/PlayMode/Network/ServerAndClientTests.unity";
        protected override IEnumerable<string> ScenePaths { get; } = new [] { TestScene };

        private Scene _scene;
        private GameObject _serverRoot;
        private Redux.IStore<GameState> _serverStore;
        private GameObject _clientRoot;
        private Redux.IStore<GameState> _clientStore;


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

                    _serverStore = _serverRoot.GetComponentInChildren<StoreContext>().Store;
                    _clientStore = _clientRoot.GetComponentInChildren<StoreContext>().Store;
                }
            );
        }

        private IEnumerator SetupServerAndClient() {
            _serverStore.Dispatch(new Network.StateAction.SetNetworkMode {
                Mode = Network.Mode.Server
            });

            _clientStore.Dispatch(new Network.StateAction.SetNetworkMode {
                Mode = Network.Mode.Client
            });

            Assert.That(_serverStore.GetState().GetNetworkMode() == Network.Mode.Server, "Server's network mode was not correctly set");
            Assert.That(_clientStore.GetState().GetNetworkMode() == Network.Mode.Client, "Client's network mode was not correctly set");

            yield return new WaitUntil(() => _clientStore.GetState().IsConnectedToServer());
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
            _serverStore.Dispatch(new Board.StateAction.AddPawn {
                PawnId = pawnId,
                PrefabKey = "NetworkedPawn"
            });

            yield return new WaitForSecondsRealtime(1f);

            Assert.That(_clientStore.GetState().HasPawn(pawnId), "Client did not receive AddPawn action");
        }

        [UnityTest]
        public IEnumerator ServerAndClientTimeMatch() {
            yield return SetupServerAndClient();
            Func<float> serverTime = _serverRoot.GetComponentInChildren<Network.NetworkAgent>().GetTime;
            Func<float> clientTime = _clientRoot.GetComponentInChildren<Network.NetworkAgent>().GetTime;

            var times = new List<(float, float)>();
            for (int i = 0; i <= 10; i++) {
                var pair = (serverTime(), clientTime());
                times.Add(pair);
                Debug.Log($"Server: {pair.Item1}, Client: {pair.Item2}, Diff: {pair.Item1 - pair.Item2}");
                yield return new WaitForSecondsRealtime(1f);
            }

            Assert.That(!times.Any(t => t.Item1 != t.Item2), $"Server and client have mismatched timestamps");
        }

        [UnityTest]
        public IEnumerator ServerInputCommand() {
            yield return SetupServerAndClient();

            var playerId = new Player.PlayerId(1);
            _serverStore.Dispatch(new Player.StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = "Local Player",
                Name = "Server Player"
            });

            var pawnId = new Pawn.PawnId(1);
            _serverStore.Dispatch(new Board.StateAction.AddPawn {
                PawnId = pawnId,
                PrefabKey = "NetworkedPawn",
                PlayerId = playerId
            });

            yield return new WaitForSeconds(1f);

            Assert.That(_clientStore.GetState().HasPawn(pawnId), "Client did not receive AddPawn action");
            Assert.AreEqual(_clientStore.GetState().GetPawnPlayerId(pawnId), playerId, "Client's copy of the pawn is not assigned to the correct Player");

            var playerInput = _serverRoot.GetComponentInChildren<Player.PlayersContext>().InputStreams;

            Func<float> serverTime = _serverRoot.GetComponentInChildren<Network.NetworkAgent>().GetTime;
            Func<float> clientTime = _clientRoot.GetComponentInChildren<Network.NetworkAgent>().GetTime;

            playerInput.PushCommand(playerId, Player.InputCommand.LightAttack, serverTime());

            yield return new WaitForSecondsRealtime(5f);

            Assert.AreEqual(serverTime(), clientTime(), $"Server time and client time don't match");

            var clientAnimator = _clientRoot.GetComponentInChildren<Animator>();
            var clientStateInfo = clientAnimator.GetCurrentAnimatorStateInfo(0);

            Assert.That(Mathf.Approximately(clientStateInfo.normalizedTime, 0.5f), "Client animator is not at the expected normalized time");
        }
    }
}
