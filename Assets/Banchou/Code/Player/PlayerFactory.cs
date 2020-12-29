using System;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UnityEngine;

using Banchou.DependencyInjection;

namespace Banchou.Player {
    public class PlayerFactory : MonoBehaviour, IPlayerInstances {
        [SerializeField] private PlayerCatalog _catalog = null;
        [SerializeField] private Transform _playerParent = null;

        private IObservable<GameState> _observeState;
        private GetState _getState;
        private Instantiator _instantiate;
        private Dictionary<PlayerId, GameObject> _instances = new Dictionary<PlayerId, GameObject>();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            _playerParent = _playerParent ?? transform;
            _observeState = observeState;
            _getState = getState;
            _instantiate = instantiate;
        }

        private void Start() {
            var observePlayersChanges = _observeState
                .Select(state => state.GetPlayerIds())
                .StartWith(Enumerable.Empty<PlayerId>())
                .DistinctUntilChanged()
                .Pairwise();

            // Create new Players based on state
            observePlayersChanges
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Where(addedId => !_instances.ContainsKey(addedId) && _getState().IsLocalPlayer(addedId))
                .CatchIgnoreLog()
                .Subscribe(addedId => {
                    var prefabKey = _getState().GetPlayerPrefabKey(addedId);
                    if (!string.IsNullOrWhiteSpace(prefabKey)) {
                        GameObject instance = null;
                        GameObject prefab;
                        if (_catalog.TryGetValue(prefabKey, out prefab)) {
                            instance = _instantiate(
                                prefab,
                                parent: _playerParent,
                                additionalBindings: addedId
                            );
                        }

                        if (instance != null) {
                            _instances[addedId] = instance;
                        }
                    }
                }).AddTo(this);

            // Remove Players based on state
            observePlayersChanges
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .CatchIgnoreLog()
                .Subscribe(removedId => {
                    GameObject instance;
                    if (_instances.TryGetValue(removedId, out instance)) {
                        GameObject.Destroy(instance);
                        _instances.Remove(removedId);
                    }
                }).AddTo(this);
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