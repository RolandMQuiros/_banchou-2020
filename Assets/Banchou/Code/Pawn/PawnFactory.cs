using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Collections;

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

        public ICollection<PawnId> Keys => throw new NotImplementedException();

        public ICollection<IPawnInstance> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public IPawnInstance this[PawnId key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Construct(
            Transform pawnParent,
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            var catalog = _prefabCatalog.ToDictionary(p => p.Key, p => p.Prefab);
            var observePawnIdDeltas = observeState
                .DistinctUntilChanged(state => state.GetPawns())
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
                        var instance = instantiate(prefab, parent: pawnParent);
                        var pawnContext = instance.GetComponent<PawnContext>();

                        if (pawnContext == null) {
                            throw new Exception($"Pawn Catalog prefab '{prefab.name}' does not contain a PawnContext at root level");
                        }

                        _instances[info.PawnId] = pawnContext;

                        // By default, pawns persist between scenes, so it's up to the State + PawnFactory to clean them up
                        GameObject.DontDestroyOnLoad(instance);
                    }
                });

            // If a pawn was removed, destroy it
            _removedSubscription?.Dispose();
            _removedSubscription = observePawnIdDeltas
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .Subscribe(id => {
                    PawnContext instance;
                    if (_instances.TryGetValue(id, out instance)) {
                        _instances.Remove(id);
                        GameObject.Destroy(instance.gameObject);
                    }
                });
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
