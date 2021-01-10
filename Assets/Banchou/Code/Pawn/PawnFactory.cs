using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UnityEngine;

using Banchou.DependencyInjection;
using Banchou.Stage;

namespace Banchou.Pawn {
    public class PawnFactory : MonoBehaviour, IPawnInstances {
        [SerializeField] private PawnCatalog _catalog = null;
        [SerializeField] private Transform _pawnParent = null;

        private IObservable<GameState> _observeState;
        private GetState _getState;
        private Instantiator _instantiate;

        private Dictionary<PawnId, PawnContext> _instances = new Dictionary<PawnId, PawnContext>();

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            _observeState = observeState;
            _getState = getState;
            _instantiate = instantiate;

            var observePawnIdDeltas = observeState
                .DistinctUntilChanged(state => state.AreScenesLoading())
                .Where(state => !state.AreScenesLoading())
                .SelectMany(
                    _ => observeState
                        .Select(state => state.GetPawnIds())
                        .DistinctUntilChanged()
                )
                .StartWith(Enumerable.Empty<PawnId>())
                .Pairwise();

            // If a pawn was added, instantiate it
            observePawnIdDeltas
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Where(id => !_instances.ContainsKey(id))
                .Select(id => (PawnId: id, Pawn: _getState().GetPawn(id)))
                .CatchIgnoreLog()
                .Subscribe(info => {
                    GameObject prefab;
                    if (_catalog.TryGetValue(info.Pawn.PrefabKey, out prefab)) {
                        var instance = _instantiate(
                            prefab,
                            parent: _pawnParent,
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
                })
                .AddTo(this);

            // If a pawn was removed, destroy it
            observePawnIdDeltas
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .CatchIgnoreLog()
                .Subscribe(id => {
                    PawnContext instance;
                    if (_instances.TryGetValue(id, out instance)) {
                        _instances.Remove(id);
                        GameObject.Destroy(instance.gameObject);
                    }
                })
                .AddTo(this);
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
