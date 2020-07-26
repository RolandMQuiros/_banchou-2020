using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

namespace Banchou.Player {
    [CreateAssetMenu(fileName = "PlayerFactory.asset", menuName = "Banchou/Player Factory")]
    public class PlayerFactory : ScriptableObject, IPlayerInstances {
        [SerializeField] private GameObject _localPlayerPrefab = null;
        [SerializeField] private GameObject _networkedPlayerPrefab = null;
        [SerializeField] private GameObject _AIPlayerPrefab = null;

        private IDisposable _addedSubscription = null;
        private IDisposable _removedSubscription = null;

        private Dictionary<PlayerId, GameObject> _instances = new Dictionary<PlayerId, GameObject>();

        public void Construct(
            Transform playerParent,
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            var observePlayersChanges = observeState
                .Select(state => state.GetPlayerIds())
                .DistinctUntilChanged()
                .Pairwise();

            // Create new Players based on state
            _addedSubscription?.Dispose();
            _addedSubscription = observePlayersChanges
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Where(addedId => !_instances.ContainsKey(addedId))
                .Subscribe(addedId => {
                    var inputSource = getState().GetPlayerInputSource(addedId);
                    GameObject instance = null;
                    switch (inputSource) {
                        case InputSource.Local:
                            instance = instantiate(_localPlayerPrefab, parent: playerParent);
                            break;
                        case InputSource.AI:
                            instance = instantiate(_AIPlayerPrefab, parent: playerParent);
                            break;
                        case InputSource.Network:
                            instance = instantiate(_networkedPlayerPrefab, parent: playerParent);
                            break;
                    }

                    if (instance != null) {
                        // Set the PlayerId for the new instance
                        var contexts = instance.GetComponentsInChildren<PlayerContext>();
                        foreach (var context in contexts) {
                            context.PlayerId = addedId;
                        }

                        // Add the instance to our dictionary
                        _instances[addedId] = instance;

                        // Player objects persist between all scene changes
                        GameObject.DontDestroyOnLoad(instance);
                    }
                });

            // Remove Players based on state
            _removedSubscription?.Dispose();
            _removedSubscription = observePlayersChanges
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .Subscribe(removedId => {
                    GameObject instance;
                    if (_instances.TryGetValue(removedId, out instance)) {
                        GameObject.Destroy(instance);
                        _instances.Remove(removedId);
                    }
                });
        }

        public GameObject Get(PlayerId playerId) {
            GameObject instance;
            if (_instances.TryGetValue(playerId, out instance)) {
                return instance;
            }
            return null;
        }

        public void Set(PlayerId playerId, GameObject gameObject) {
            var playerContext = gameObject.GetComponent<PlayerContext>();
            if (playerContext == null) {
                throw new ArgumentException("A GameObject must have a PlayerContext to be registered with PlayerInstances", "gameObject");
            }

            if (_instances.ContainsKey(playerId)) {
                throw new ArgumentException($"A PlayerInstance assigned to player ID {playerId} already exists", "playerId");
            }

            _instances.Add(playerId, gameObject);
        }
    }
}