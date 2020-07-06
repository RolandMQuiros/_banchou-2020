using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        // TODO: Make this serializable to disk so the history can persist between recompiiles
        public class Step {
            public object Action;
            public Type ActionType;
            public int CollapsedCount;
            public object State;
            public float When;

            public JToken ActionCache;
            public JToken StateCache;
            public JToken DiffCache;

            public Step() { }
            public Step(in Step prev) {
                Action = prev.Action;
                ActionType = prev.ActionType;
                CollapsedCount = prev.CollapsedCount;
                State = prev.State;
                When = prev.When;
            }
        }

        [SerializeField] private bool _isRecording = false;
        private HashSet<Type> _actionsDisplayFilter = new HashSet<Type>();
        private List<Type> _actionTypes = new List<Type>();
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
                        _instance.ClearHistory();
                    }

                    if (_instance?._isRecording == true) {
                        _instance.RecordStep(action, store.GetState());
                    }

                    return dispatched;
                };
            }

            // Return a pass-through method if we're not in-editor
            return next => next;
        }
        #endregion

        public void AddStep(Step step) {
            _history.Add(step);
            if (_historyList.index == _history.Count - 2) {
                _historyList.index = _history.Count - 1;
                _historyScroll.y = _historyList.GetHeight();
                RefreshView(_historyList.index);
            }
        }

        public void RecordStep(object action, object state) {
            var history = _instance._history;

            // Early-out if the action is a thunk, or filtered
            var actionType = action.GetType();
            if (action is Delegate || _instance._actionsDisplayFilter.Contains(actionType)) {
                return;
            }

            var step = new Step {
                Action = action,
                ActionType = actionType,
                State = state,
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
                        history[history.Count - 1] = new Step(prevStep) {
                            Action = new List<Step> { prevStep, step },
                            State = state,
                            When = Time.unscaledTime,
                            CollapsedCount = 2
                        };
                    } else {
                        (prevStep.Action as List<Step>).Add(step);
                        prevStep.CollapsedCount++;
                    }

                    return;
                }
            }
            _instance.AddStep(step);
        }

        public void ClearHistory() {
            _history.Clear();
            _treeView.Reload();
        }

        #region GUI
        private ReorderableList _historyList = null;
        private TreeViewState _viewState = null;
        private JSONTreeView _treeView = null;
        private JsonSerializer _serializer;

        private void OnEnable() {
            _actionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !string.IsNullOrWhiteSpace(type.Namespace) && type.Namespace.EndsWith("StateAction"))
                .ToList();

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
            if (index < 0 || _history.Count < index) { return; }
            var step = _history[index];

            switch (_viewMode) {
                case 0: { // View Action
                    var collapsedActions = step.Action as List<Step>;
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
                        var prev = _history[index - 1];
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
            _treeView.SetExpandedRecursive(0, true);
        }

        private Vector2 _historyScroll = Vector2.zero;
        private Vector2 _viewScroll = Vector2.zero;
        private Rect _fullRect = Rect.zero;
        private Rect _historyRect = Rect.zero;
        private int _viewMode = 0;

        private void OnGUI() {
            void ApplyFilter() {
                _historyList.list = _history
                    .Where(step => !_actionsDisplayFilter.Contains(step.ActionType))
                    .ToList();
            }

            _isRecording = EditorGUILayout.Toggle(label: "Recording", value: _isRecording);

            var fullRect = EditorGUILayout.BeginHorizontal();
            _fullRect = fullRect == Rect.zero ? _fullRect : fullRect;

                EditorGUILayout.BeginVertical(GUILayout.Width(_fullRect.width * 0.25f));

                    // Draw history + clear button header
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("History", new GUIStyle { fontStyle = FontStyle.Bold });
                        if (GUILayout.Button("Clear")) {
                            _history.Clear();
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