using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UniRx.Diagnostics;
using UnityEngine;

using Banchou.DependencyInjection;

namespace Banchou.Pawn {
    [CreateAssetMenu(fileName = "PawnFactory.asset", menuName = "Banchou/Pawn Factory")]
    public class PawnFactory : ScriptableObject, IPawnInstances {
        [Serializable]
        private class PrefabPair {
            public string Key = null;
            public GameObject Prefab = null;
        }
        [SerializeField] private PrefabPair[] _prefabCatalog = null;
        private Dictionary<PawnId, PawnContext> _instances = new Dictionary<PawnId, PawnContext>();
        private IDisposable _addedSubscription = null;
        private IDisposable _removedSubscription = null;

        public void Construct(
            Transform pawnParent,
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            var catalog = _prefabCatalog.ToDictionary(p => p.Key, p => p.Prefab);
            var observePawnIdDeltas = observeState
                .DistinctUntilChanged(state => state?.GetPawns())
                .Where(state => state != null)
                .Select(state => state.GetPawnIds())
                .Pairwise();

            // If a pawn was added, instantiate it
            _addedSubscription?.Dispose();
            _addedSubscription = observePawnIdDeltas
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Where(id => !_instances.ContainsKey(id))
                .Select(id => (PawnId: id, Pawn: getState().GetPawn(id)))
                .Subscribe(info => {
                    GameObject prefab;
                    if (catalog.TryGetValue(info.Pawn.PrefabKey, out prefab)) {
                        var instance = instantiate(
                            prefab,
                            parent: pawnParent,
                            position: info.Pawn.SpawnPosition,
                            rotation: info.Pawn.SpawnRotation,
                            additionalBindings: info.PawnId
                        );
                        var pawnContext = instance.GetComponent<PawnContext>();

                        if (pawnContext == null) {
                            throw new Exception($"Pawn Catalog prefab '{prefab.name}' does not contain a PawnContext at root level");
                        }

                        _instances[info.PawnId] = pawnContext;
                    }
                });

            // If a pawn was removed, destroy it
            _removedSubscription?.Dispose();
            _removedSubscription = observePawnIdDeltas
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .CatchIgnoreLog()
                .Subscribe(id => {
                    PawnContext instance;
                    if (_instances.TryGetValue(id, out instance)) {
                        _instances.Remove(id);
                        GameObject.Destroy(instance.gameObject);
                    }
                });
        }

        public IEnumerable<string> GetPrefabKeys() {
            return _prefabCatalog.Select(p => p.Key);
        }

        public GameObject GetPrefab(string prefabKey) {
            return _prefabCatalog
                .Where(c => c.Key == prefabKey)
                .Select(c => c.Prefab)
                .FirstOrDefault();
        }

        public IPawnInstance Get(PawnId pawnId) {
            PawnContext instance;
            if (_instances.TryGetValue(pawnId, out instance)) {
                return instance;
            }
            return null;
        }

        public IEnumerable<IPawnInstance> GetMany(IEnumerable<PawnId> pawnIds) {
            foreach (var pawnId in pawnIds) {
                yield return Get(pawnId);
            }
        }

        public void Set(PawnId pawnId, IPawnInstance instance) {
            var pawnContext = instance as PawnContext;
            if (pawnContext != null) {
                _instances.Add(pawnId, pawnContext);
            }
        }

        public IEnumerator<IPawnInstance> GetEnumerator() => _instances.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _instances.Values.GetEnumerator();
    }
}
