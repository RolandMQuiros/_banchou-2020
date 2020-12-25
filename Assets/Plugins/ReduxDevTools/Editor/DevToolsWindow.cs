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
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using JsonDiffPatchDotNet;

namespace Redux.DevTools {
    public class DevToolsWindow : EditorWindow {
        [SerializeField] private DevToolsSession _session;
        private HashSet<Type> _actionsDisplayFilter = new HashSet<Type>();
        private List<Type> _actionTypes = new List<Type>();

        [MenuItem("Tools/Redux Dev Tools")]
        public static void ShowWindow() {
            EditorWindow.CreateWindow<DevToolsWindow>(
                title: "Redux DevTools"
            );
        }

        #region GUI
        private ReorderableList _historyList = null;
        private TreeViewState _viewState = null;
        private JSONTreeView _treeView = null;
        private JsonSerializer _serializer;

        private void OnEnable() {
            // Create a custom serializer for Unity classes that don't handle it well by default
            _serializer = JsonSerializer.Create(JsonConvert.DefaultSettings());
            _serializer.Converters.Add(new StringEnumConverter());
            _serializer.TypeNameHandling = TypeNameHandling.All;

            if (_session == null) {
                var searchForSession = AssetDatabase.FindAssets("t:DevToolsSession");
                var guid = searchForSession.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(guid)) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    _session = AssetDatabase.LoadAssetAtPath<DevToolsSession>(path);
                } else {
                    Debug.LogWarning("No Redux DevToolsSession asset found");
                }
            }

            if (_session != null) {
                AttachToSession(_session);
            }
        }

        private void AttachToSession(DevToolsSession session) {
            var history = session.History;

            session.OnAdd += step => {
                if (_historyList.index == history.Count - 2) {
                    _historyList.index = history.Count - 1;
                    _historyScroll.y = _historyList.GetHeight();
                    RefreshView(_historyList.index);
                }
            };

            session.OnClear += () => { _treeView.Reload(); };

            _actionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace) && type.Namespace.EndsWith("StateAction"))
                .ToList();

            _historyList = new ReorderableList(
                history,
                typeof(DevToolsSession.Step),
                draggable: false,
                displayHeader: false,
                displayAddButton: false,
                displayRemoveButton: false
            );

            _historyList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var step = history[index];

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
                var listHeight = list.GetHeight();
                if (_historyScroll.y >= listHeight) {
                    _historyScroll.y = listHeight;
                }
            };

            _viewState = _viewState ?? new TreeViewState();
            _treeView = new JSONTreeView(_viewState);
            _treeView.Reload();

            _session = session;
            titleContent = new GUIContent($"Redux DevTools [{_session.name}]");
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
            var history = _session.History;

            if (index < 0 || history.Count < index) { return; }
            var step = history[index];

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
                        var prev = history[index - 1];
                        prev.StateCache = prev.StateCache ?? JToken.FromObject(prev.State, _serializer);
                        step.StateCache = step.StateCache ?? JToken.FromObject(step.State, _serializer);
                        step.DiffCache = step.DiffCache ?? new JsonDiffPatch().Diff(step.StateCache, prev.StateCache);

                        _treeView.Source = step.DiffCache;
                    } else {
                        _treeView.Source = new JObject();
                    }
                } break;
            }

            _stackTrace = Regex.Replace(step.StackTrace, ":([0-9]+)\\s*$", ":<color=#00ffc8>$1</color>\n", RegexOptions.Multiline);
            _treeView.Reload();
            _treeView.SetExpanded(0, true);
        }

        private Vector2 _historyScroll = Vector2.zero;
        private Vector2 _viewScroll = Vector2.zero;
        private Vector2 _stackTraceScroll = Vector2.zero;
        private Rect _mainRect = Rect.zero;
        private Rect _historyRect = Rect.zero;
        private Rect _footerRect = Rect.zero;
        private int _viewMode = 0;
        private string _stackTrace = null;

        private void OnGUI() {
            var session = (DevToolsSession)EditorGUILayout.ObjectField(_session, typeof(DevToolsSession), false);

            if (session != null && session != _session) {
                AttachToSession(session);
            }

            if (_session == null) {
                return;
            }

            _session.IsRecording = EditorGUILayout.Toggle(label: "Recording", value: _session.IsRecording);
            var history = _session.History;

            var mainRect = EditorGUILayout.BeginHorizontal();
            _mainRect = mainRect == Rect.zero ? _mainRect : mainRect;

                EditorGUILayout.BeginVertical(GUILayout.Width(_mainRect.width * 0.25f));

                    // Draw history + clear button header
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("History");
                        if (GUILayout.Button("Clear")) {
                            history.Clear();
                            _treeView.Source = null;
                            _treeView.Reload();
                        }
                    EditorGUILayout.EndHorizontal();

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

                    if (_stackTrace != null) {
                        var footerRect = EditorGUILayout.BeginVertical();
                            EditorGUILayout.LabelField("Dispatch Stack Trace", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                            _footerRect = footerRect == Rect.zero ? _footerRect : footerRect;
                                _stackTraceScroll = EditorGUILayout.BeginScrollView(
                                    scrollPosition: _stackTraceScroll,
                                    alwaysShowHorizontal: false,
                                    alwaysShowVertical: true,
                                    GUILayout.Height(_mainRect.height * 0.35f)
                                );
                                    EditorGUILayout.TextArea(
                                        _stackTrace,
                                        new GUIStyle(GUI.skin.label) {
                                            wordWrap = true,
                                            richText = true
                                        },
                                        GUILayout.ExpandHeight(true)
                                    );
                                EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndVertical();
                    }
                EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate() {
            Repaint();
        }
        #endregion
    }
}