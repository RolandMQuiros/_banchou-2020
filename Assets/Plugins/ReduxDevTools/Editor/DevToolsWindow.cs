/*
MIT License

Copyright (c) 2020 Roland Quiros

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonDiffPatchDotNet;

namespace Redux.DevTools {
    public class DevToolsWindow : EditorWindow {
        private DevToolsSession _session = null;
        private ReorderableList _historyList = null;
        private TreeViewState _viewState = null;
        private JSONTreeView _treeView = null;
        private JsonSerializer _serializer;
        private List<Type> _actionTypes = null;
        private HashSet<Type> _actionsDisplayFilter = new HashSet<Type>();

        private Vector2 _historyScroll = Vector2.zero;
        private Vector2 _viewScroll = Vector2.zero;
        private Rect _fullRect = Rect.zero;
        private Rect _historyRect = Rect.zero;
        private int _viewMode = 0;


        [MenuItem("Tools/Redux Dev Tools")]
        public static void ShowWindow() {
            EditorWindow.GetWindow<DevToolsWindow>(
                title: "Redux DevTools"
            );
        }

        private void OnEnable() {
            _actionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace) && type.Namespace.EndsWith("StateAction"))
                .ToList();

            // Create a custom serializer for Unity classes that don't handle it well by default
            _serializer = JsonSerializer.Create(JsonConvert.DefaultSettings());
            _serializer.TypeNameHandling = TypeNameHandling.All;

            var path = AssetDatabase.FindAssets("t:DevToolsSession")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .FirstOrDefault();

            if (path == null) {
                path = "Assets/ReduxDevToolsSession.asset";
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DevToolsSession>(), path);
            }

            AttachToSession(AssetDatabase.LoadAssetAtPath<DevToolsSession>(path));
        }

        private void AttachToSession(DevToolsSession session) {
            session.OnAdd += step => {
                if (_historyList.index == session.History.Count - 2) {
                    _historyList.index = session.History.Count - 1;
                    _historyScroll.y = _historyList.GetHeight();
                    RefreshView(_historyList.index);
                }
            };

            session.OnClear += () => { _treeView.Reload(); };
            session.OnCollapse += (_, index) => { RefreshView(index); };

            _historyList = new ReorderableList(
                session.History,
                typeof(DevToolsSession.Step),
                draggable: false,
                displayHeader: false,
                displayAddButton: false,
                displayRemoveButton: false
            );

            _historyList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var step = session.History[index];

                // Display the name of the action type
                var nameRect = new Rect(rect) {
                    width = rect.width * 0.75f
                };
                GUI.Label(nameRect, step.ActionType.Name + (step.CollapsedCount > 0 ? $" x{step.CollapsedCount + 1}" : ""));

                // Display the uptime, in seconds, when the action was dispatched
                var dateRect = new Rect(rect) {
                    width = rect.width * 0.25f,
                    x = rect.x + rect.width * 0.75f
                };
                GUI.Label(dateRect, step.When.ToString("n2"));
            };

            _historyList.onSelectCallback = list => { RefreshView(list.index); };

            _historyList.onChangedCallback = list => {
                Debug.Log("Changed?");
                var listHeight = list.GetHeight();
                if (_historyScroll.y >= listHeight) {
                    _historyScroll.y = listHeight;
                }
            };

            _viewState = _viewState ?? new TreeViewState();
            _treeView = new JSONTreeView(_viewState);
            _treeView.Reload();

            _session = session;
        }

        private void DrawActionsMenu(HashSet<Type> target, Action<Type> onAdd = null, Action<Type> onRemove = null) {
            var menu = new GenericMenu();
            for (int i = 0; i < _actionTypes.Count; i++) {
                var type = _actionTypes[i];
                var path = type.FullName
                    .Replace("StateAction.", "")
                    .Replace(".", "/");

                menu.AddItem(new GUIContent(path), target.Contains(type), () => {
                    if (target.Contains(type)) {
                        target.Remove(type);
                        onRemove?.Invoke(type);
                    } else {
                        target.Add(type);
                        onAdd?.Invoke(type);
                    }
                });
            }
            menu.ShowAsContext();
        }

        private void RefreshView(int index) {
            if (index < 0 || _session.History.Count < index) { return; }
            var step = _session.History[index];

            switch (_viewMode) {
                case 0: { // View Action
                    var collapsedActions = step.Action as List<DevToolsSession.Step>;
                    if (collapsedActions != null) {
                        _treeView.Source = step.ActionCache =
                            step.ActionCache ??
                            JToken.FromObject(
                                collapsedActions.Select(c => c.Action),
                                _serializer
                            );
                    } else {
                        _treeView.Source = step.ActionCache = step.ActionCache ?? JToken.FromObject(step.Action, _serializer);
                    }
                } break;
                case 1: { // View State
                    _treeView.Source = step.StateCache = step.StateCache ?? JToken.FromObject(step.State, _serializer);
                } break;
                case 2: { // View State diff
                    if (index > 0) {
                        var prev = _session.History[index - 1];
                        prev.StateCache = prev.StateCache ?? JToken.FromObject(prev.State, _serializer);
                        step.StateCache = step.StateCache ?? JToken.FromObject(step.State, _serializer);
                        step.DiffCache = step.DiffCache ?? new JsonDiffPatch().Diff(step.StateCache, prev.StateCache);

                        _treeView.Source = step.DiffCache;
                    } else {
                        _treeView.Source = new JObject();
                    }
                } break;
            }

            _treeView.Reload();
            _treeView.SetExpanded(0, true);
        }

        private void OnGUI() {
            void ApplyFilter() {
                _historyList.list = _session.History
                    .Where(step => !_actionsDisplayFilter.Contains(step.ActionType))
                    .ToList();
            }

            if (_session == null) {
                GUILayout.Label("A DevToolsSession asset was not found, or could not be created in this project");
                return;
            }

            _session.IsRecording = EditorGUILayout.Toggle(label: "Recording", value: _session.IsRecording);

            var fullRect = EditorGUILayout.BeginHorizontal();
            _fullRect = fullRect == Rect.zero ? _fullRect : fullRect;

                EditorGUILayout.BeginVertical(GUILayout.Width(_fullRect.width * 0.25f));

                    // Draw history + clear button header
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("History", new GUIStyle { fontStyle = FontStyle.Bold });
                        if (GUILayout.Button("Clear")) {
                            _session.Clear();
                            _treeView.Source = null;
                            _treeView.Reload();
                        }
                    EditorGUILayout.EndHorizontal();

                    // Draw Action History filter
                    if (GUILayout.Button("Filter")) {
                        DrawActionsMenu(
                            _actionsDisplayFilter,
                            onAdd: _ => ApplyFilter(),
                            onRemove: _ => ApplyFilter()
                        );
                    }

                    // Draw Action history scrollview
                    _historyScroll = EditorGUILayout.BeginScrollView(
                        scrollPosition: _historyScroll,
                        alwaysShowHorizontal: false,
                        alwaysShowVertical: true
                    );
                        _historyList.DoLayoutList();
                    EditorGUILayout.EndScrollView();

                    if (GUILayout.Button("To Bottom")) {
                        _historyScroll.y = _historyList.GetHeight();
                    }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                    var viewMode = GUILayout.SelectionGrid(_viewMode, new [] { "Action", "State", "Diff" }, 3);
                    if (viewMode != _viewMode) {
                        _viewMode = viewMode;
                        RefreshView(_historyList.index);
                    }

                    _viewScroll = EditorGUILayout.BeginScrollView(
                        scrollPosition: _viewScroll,
                        alwaysShowHorizontal: false,
                        alwaysShowVertical: false
                    );
                        var viewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        _treeView.OnGUI(viewRect);
                    EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate() {
            Repaint();
        }
    }
}