using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou.Pawn {
    [CreateAssetMenu(fileName = "Pawn Catalog.asset", menuName = "Banchou/Pawn Catalog")]
    public class PawnCatalog : ScriptableObject, IDictionary<string, GameObject> {
        [Serializable]
        private class PrefabPair {
            public string Key = null;
            public GameObject Prefab = null;
        }
        [SerializeField] private PrefabPair[] _prefabCatalog = null;
        private Dictionary<string, GameObject> _runtimeCatalog;

        private void OnEnable() {
            _runtimeCatalog = _prefabCatalog.ToDictionary(p => p.Key, p => p.Prefab);
        }

        #region IDictionary facade
            public GameObject this[string key] { get => ((IDictionary<string, GameObject>)_runtimeCatalog)[key]; set => ((IDictionary<string, GameObject>)_runtimeCatalog)[key] = value; }
            public ICollection<string> Keys => ((IDictionary<string, GameObject>)_runtimeCatalog).Keys;
            public ICollection<GameObject> Values => ((IDictionary<string, GameObject>)_runtimeCatalog).Values;
            public int Count => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).Count;
            public bool IsReadOnly => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).IsReadOnly;
            public void Add(string key, GameObject value) => ((IDictionary<string, GameObject>)_runtimeCatalog).Add(key, value);
            public void Add(KeyValuePair<string, GameObject> item) => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).Add(item);
            public void Clear() => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).Clear();
            public bool Contains(KeyValuePair<string, GameObject> item) => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).Contains(item);
            public bool ContainsKey(string key) => ((IDictionary<string, GameObject>)_runtimeCatalog).ContainsKey(key);
            public void CopyTo(KeyValuePair<string, GameObject>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, GameObject>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, GameObject>>)_runtimeCatalog).GetEnumerator();
            public bool Remove(string key) => ((IDictionary<string, GameObject>)_runtimeCatalog).Remove(key);
            public bool Remove(KeyValuePair<string, GameObject> item) => ((ICollection<KeyValuePair<string, GameObject>>)_runtimeCatalog).Remove(item);
            public bool TryGetValue(string key, out GameObject value) => ((IDictionary<string, GameObject>)_runtimeCatalog).TryGetValue(key, out value);
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_runtimeCatalog).GetEnumerator();
        #endregion
    }
}