using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Banchou.Pawn.UnityEditor {
    [CustomEditor(typeof(PawnSpawner))]
    public class PawnSpawnerInspector : Editor {
        private PawnSpawner _target;
        private SerializedProperty _prefabKeyProperty;
        private PawnFactory _pawnFactory;
        private string[] _prefabKeys;
        private (Mesh, Transform, Material)[] _previewMeshes;

        private void OnEnable() {
            _target = (PawnSpawner)target;
            _prefabKeyProperty = serializedObject.FindProperty("_prefabKey");

            var factoryPath = AssetDatabase.FindAssets("t:PawnFactory")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .FirstOrDefault();

            if (factoryPath != null) {
                _pawnFactory = AssetDatabase.LoadAssetAtPath<PawnFactory>(factoryPath);
                _prefabKeys = _pawnFactory.GetPrefabKeys().ToArray();
            }

        }

        public override void OnInspectorGUI() {
            if (_prefabKeys == null) {
                EditorGUILayout.LabelField("No PawnFactory asset was found in this project");
            } else {
                EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Prefab Key", GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(_prefabKeyProperty.stringValue, GUILayout.ExpandWidth(true))) {
                        var prefabMenu = new GenericMenu();
                        for (int i = 0; i < _prefabKeys.Length; i++) {
                            var key = _prefabKeys[i];
                            prefabMenu.AddItem(new GUIContent(key), _prefabKeyProperty.stringValue == key, () => {
                                _prefabKeyProperty.stringValue = key;
                                serializedObject.ApplyModifiedProperties();
                                SetViewModel(key);
                            });
                        }
                        prefabMenu.ShowAsContext();
                    }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void SetViewModel(string prefabKey) {
            var prefab = _pawnFactory.GetPrefab(prefabKey);
            if (prefab != null) {
                _previewMeshes = prefab
                    .GetComponentsInChildren<MeshFilter>()
                    .Select(f => (f.sharedMesh, f.transform, f.GetComponent<Renderer>()?.material ))
                    .ToArray();
            }
        }

        // private void OnSceneGUI() {
        //     if (_previewMeshes != null) {
        //         for (int i = 0; i < _previewMeshes.Length; i++) {
        //             var (mesh, transform, material) = _previewMeshes[i];
        //             for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++) {
        //                 Graphics.DrawMeshInstanced(
        //                     mesh,
        //                     subMeshIndex,
        //                     material,
        //                     new [] { transform.localToWorldMatrix, _target.transform.localToWorldMatrix }
        //                 );
        //             }
        //         }
        //     }
        // }
    }
}