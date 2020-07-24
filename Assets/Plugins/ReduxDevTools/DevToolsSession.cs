using System;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json.Linq;

namespace Redux.DevTools {
    public class DevToolsSession : ScriptableObject {
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
        [HideInInspector] public bool IsRecording = false;
        [NonSerialized] public List<Step> History = new List<Step>();
        [NonSerialized] public Dispatcher Dispatch = null;

        public event Action<Step> OnAdd;
        public event Action<Step, int> OnCollapse;
        public event Action OnClear;

        public void Clear() {
            History.Clear();
            OnClear?.Invoke();
        }

        public void RecordStep(object action, object state) {
            // Early-out if the action is a thunk, or filtered
            var actionType = action.GetType();
            if (action is Delegate) {
                return;
            }

            var step = new Step {
                Action = action,
                ActionType = actionType,
                State = state,
                When = Time.unscaledTime
            };

            if (History.Count > 0) {
                var prevStep = History[History.Count - 1];

                // Action classes marked with the [CollapsibleAction] attribute are aggregated into
                // Lists of actions and are displayed in the devtools as a single row. This is useful for
                // actions that are dispatched very often, like continuous controller input
                if (action is ICollapsibleAction && prevStep.ActionType == actionType) {
                    // If the previous step action is collapsible and of the same type, collapse the current
                    // action into it
                    History[History.Count - 1] = new Step(prevStep) {
                        Action = ((ICollapsibleAction)prevStep.Action).Collapse(action),
                        State = state,
                        When = Time.unscaledTime,
                        CollapsedCount = prevStep.CollapsedCount + 1
                    };

                    OnCollapse?.Invoke(step, History.Count - 1);
                    return;
                }
            }

            History.Add(step);
            OnAdd?.Invoke(step);
        }

        public Middleware<TState> Install<TState>() {
            if (Application.isEditor) {
                return store => next => action => {
                    var dispatched = next(action);
                    if (IsRecording) {
                        RecordStep(action, store.GetState());
                    }
                    return dispatched;
                };
            }
            // Return a pass-through method if we're not in-editor
            return store => next => next;
        }
    }
}
