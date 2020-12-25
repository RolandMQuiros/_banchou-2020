using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Banchou.Test {
    public class SceneTest : IPrebuildSetup, IPostBuildCleanup {
        private EditorBuildSettingsScene[] _oldScenes;
        protected virtual IEnumerable<string> ScenePaths { get; }

        public void Setup() {
            #if UNITY_EDITOR
                _oldScenes = EditorBuildSettings.scenes;
                EditorBuildSettings.scenes = _oldScenes.Concat(
                    ScenePaths.Select(testScene => new EditorBuildSettingsScene(testScene, true))
                ).ToArray();
            #endif
        }

        public void Cleanup() {
            #if UNITY_EDITOR
                EditorBuildSettings.scenes = _oldScenes;
            #endif
        }

        public void LoadScene(string sceneNameOrPath, LoadSceneMode loadMode, Action<Scene, LoadSceneMode> onLoaded) {
            #if UNITY_EDITOR
                void OnLoaded(Scene scene, LoadSceneMode mode) {
                    if (onLoaded != null) {
                        EditorSceneManager.sceneLoaded -= OnLoaded;
                        onLoaded(scene, mode);
                    }
                }

                EditorSceneManager.sceneLoaded += OnLoaded;
                EditorSceneManager.LoadSceneInPlayMode(sceneNameOrPath, new LoadSceneParameters(loadMode));
            #else
                void OnLoaded(Scene scene, LoadSceneMode mode) {
                    if (onLoaded != null) {
                        SceneManager.sceneLoaded -= OnLoaded;
                        onLoaded(scene, mode);
                    }
                }

                SceneManager.sceneLoaded += OnLoaded;
                SceneManager.LoadScene(sceneNameOrPath, loadMode);
            #endif
        }

        public IEnumerator LoadSceneAsync(string sceneNameOrPath, LoadSceneMode loadMode, Action<Scene, LoadSceneMode> onLoaded) {
            #if UNITY_EDITOR
                void OnLoaded(Scene scene, LoadSceneMode mode) {
                    if (onLoaded != null) {
                        EditorSceneManager.sceneLoaded -= OnLoaded;
                        onLoaded(scene, mode);
                    }
                }

                EditorSceneManager.sceneLoaded += OnLoaded;
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(sceneNameOrPath, new LoadSceneParameters(loadMode));
            #else
                void OnLoaded(Scene scene, LoadSceneMode mode) {
                    if (onLoaded != null) {
                        SceneManager.sceneLoaded -= OnLoaded;
                        onLoaded(scene, mode);
                    }
                }

                SceneManager.sceneLoaded += OnLoaded;
                yield return SceneManager.LoadSceneAsync(sceneNameOrPath, loadMode);
            #endif
        }
    }
}