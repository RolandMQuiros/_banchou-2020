using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonDiffPatchDotNet;

namespace Redux.UnityEditor {
    public class DevTools : EditorWindow {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        public class CollapsibleActionAttribute : Attribute { }

        public class Step {
            public object Action;
            public Type ActionType;
            public int CollapsedCount;
            public object State;
            public float When;

            public JToken ActionCache;
            public JToken StateCache;
            public JToken DiffCache;
        }

        [SerializeField] private bool _isRecording = false;
        private List<Step> _history = new List<Step>();
        private Dispatcher _dispatch = null;


        #region Singleton stuff
        private static DevTools _instance;

        [MenuItem("Tools/Redux Dev Tools")]
        public static void ShowWindow() {
            GetWindow();
        }

        private static DevTools GetWindow() {
            return EditorWindow.GetWindow<DevTools>(
                title: "Redux DevTools"
            );
        }

        public static Func<Dispatcher, Dispatcher> Middleware<TState>(IStore<TState> store) {
            if (Application.isEditor) {
                return next => action => {
                    var dispatched = next(action);

                    if (_instance == null && EditorWindow.HasOpenInstances<DevTools>()) {
                        _instance = GetWindow();
                        _instance._dispatch = store.Dispatch;
                        _instance._history.Clear();
                        _instance._treeView.Reload();
                    }

                    if (_instance?._isRecording == true) {
                        var history = _instance._history;

                        // Early-out if the action is a thunk
                        if (action is Delegate) {
                            return dispatched;
                        }

                        var actionType = action.GetType();
                        var step = new Step {
                            Action = action,
                            ActionType = actionType,
                            State = store.GetState(),
                            When = Time.unscaledTime
                        };

                        if (history.Count > 0) {
                            var prevStep = history[history.Count - 1];

                            // Action classes marked with the [CollapsibleAction] attribute are aggregated into
                            // Lists of actions and are displayed in the devtools as a single row. This is useful for
                            // actions that are dispatched very often, like continuous controller input
                            var isCollapsible = actionType
                                .GetCustomAttribute<CollapsibleActionAttribute>() != null;
                            if (isCollapsible && prevStep.ActionType == actionType) {
                                // If the previous step action is collapsible and of the same type, turn it into a list
                                // and add the current action to it
                                if (prevStep.CollapsedCount == 0) {
                                    prevStep.Action = new List<object> {
                                        prevStep.Action,
                                        action
                                    };
                                } else {
                                    (prevStep.Action as List<object>).Add(action);
                                }

                                prevStep.State = store.GetState();
                                prevStep.When = Time.unscaledTime;
                                prevStep.CollapsedCount++;

                                return dispatched;
                            }
                        }
                        history.Add(step);
                    }

                    return dispatched;
                };
            }

            // Return a pass-through method if we're not in-editor
            return next => next;
        }
        #endregion

        #region GUI
        private ReorderableList _historyList = null;
        private TreeViewState _viewState = null;
        private JSONTreeView _treeView = null;
        private JsonSerializer _serializer;

        private void OnEnable() {
            _historyList = new ReorderableList(
                _history,
                typeof(Step),
                draggable: false,
                displayHeader: false,
                displayAddButton: false,
                displayRemoveButton: false
            );

            // Create a custom serializer for Unity classes that don't handle it well by default
            _serializer = new JsonSerializer();
            _serializer.Converters.Add(new Vec2Conv());
            _serializer.Converters.Add(new Vec3Conv());
            _serializer.Converters.Add(new Vec4Conv());
            _serializer.TypeNameHandling = TypeNameHandling.All;

            _historyList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var step = _history[index];

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
        }

        private void RefreshView(int index) {
            if (index < 0 || _history.Count < index) { return; }
            var step = _history[index];

            switch (_viewMode) {
                case 0: { // View Action
                    _treeView.Source = step.ActionCache = step.ActionCache ?? JToken.FromObject(step.Action, _serializer);
                } break;
                case 1: { // View State
                    _treeView.Source = step.StateCache = step.StateCache ?? JToken.FromObject(step.State, _serializer);
                } break;
                case 2: { // View State diff
                    if (index > 0) {
                        var prev = _history[index - 1];
                        prev.StateCache = prev.StateCache ?? JToken.FromObject(prev.State, _serializer);
                        step.StateCache = step.StateCache ?? JToken.FromObject(step.State, _serializer);
                        step.DiffCache = step.DiffCache ?? new JsonDiffPatch().Diff(prev.StateCache, step.StateCache);

                        _treeView.Source = step.DiffCache;
                    } else {
                        _treeView.Source = new JObject();
                    }
                } break;
            }

            _treeView.Reload();
            _treeView.SetExpandedRecursive(0, true);
        }

        private Vector2 _historyScroll = Vector2.zero;
        private Vector2 _viewScroll = Vector2.zero;
        private Rect _fullRect = Rect.zero;
        private Rect _historyRect = Rect.zero;
        private int _viewMode = 0;

        private void OnGUI() {
            _isRecording = EditorGUILayout.Toggle(label: _isRecording ? "End Recording" : "Begin Recording", value: _isRecording);

            var fullRect = EditorGUILayout.BeginHorizontal();
            _fullRect = fullRect == Rect.zero ? _fullRect : fullRect;

                EditorGUILayout.BeginVertical(GUILayout.Width(_fullRect.width * 0.25f));
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("History", new GUIStyle { fontStyle = FontStyle.Bold });
                        if (GUILayout.Button("Clear")) {
                            _history.Clear();
                            _treeView.Source = null;
                            _treeView.Reload();
                        }
                    EditorGUILayout.EndHorizontal();
                    _historyScroll = EditorGUILayout.BeginScrollView(
                        scrollPosition: _historyScroll,
                        alwaysShowHorizontal: false,
                        alwaysShowVertical: true
                    );
                        _historyList.DoLayoutList();
                    EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                    var viewMode = GUILayout.SelectionGrid(_viewMode, new [] { "Action", "State", "Diff" }, 3);
                    if (viewMode != _viewMode) {
                        _viewMode = viewMode;
                        RefreshView(_historyList.index);
                    }

                    _viewScroll = EditorGUILayout.BeginScrollView(
                        scrollPosition: _historyScroll,
                        alwaysShowHorizontal: false,
                        alwaysShowVertical: false
                    );
                        var viewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);

                        _treeView.OnGUI(viewRect);
                    EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate() {
            Repaint();
        }

        #endregion
    }
}