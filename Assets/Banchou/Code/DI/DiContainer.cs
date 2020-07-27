using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou.DependencyInjection {
    public sealed class DiContainer {
        private class Binding {
            public object Instance = null;
            public Func<Type, bool> Condition = null;
        }

        private Dictionary<Type, Binding> _bindings = new Dictionary<Type, Binding>();

        public DiContainer(params object[] bindings) {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);

            foreach (var binding in bindings) {
                _bindings[binding.GetType()] = new Binding {
                    Instance = binding
                };
            }
        }

        public DiContainer(in DiContainer prev, params object[] bindings) {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);

            foreach (var binding in bindings) {
                _bindings[binding.GetType()] = new Binding {
                    Instance = binding
                };
            }
        }

        public void Bind<T>(T instance) {
            _bindings[typeof(T)] = new Binding { Instance = instance };
        }

        public void Bind<T>(T instance, Func<Type, bool> where) {
            _bindings[typeof(T)] = new Binding {
                Instance = instance,
                Condition = where
            };
        }

        public void Inject(object target) {
            if (target.GetType() == typeof(DiContainer)) {
                throw new Exception("Cannot inject into a DiContainer");
            }

            var applicableBindings = _bindings
                .Where(pair => pair.Value.Condition == null || pair.Value.Condition(target.GetType()))
                .Select(pair => (Key: pair.Key, Value: pair.Value.Instance));

            if (!applicableBindings.Any()) {
                return;
            }

            var targetType = target.GetType();

            var injectInfo = targetType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(inject => inject.Name == "Construct")
                .Cast<MethodBase>();

            foreach (var inject in injectInfo) {
                var parameters = inject.GetParameters();
                var injectionPairs = parameters
                    // Left join against the parameter list
                    .GroupJoin(
                        inner: applicableBindings,
                        outerKeySelector: parameter => parameter.ParameterType,
                        innerKeySelector: pair => pair.Key,
                        resultSelector: (parameter, containerPairs) => (
                            Parameter: parameter, Values: containerPairs.Select(pair => pair.Value)
                        )
                    )
                    .Select(pair => {
                        var (parameter, values) = pair;
                        // If the Construct method has a default value, and there's no binding available, use the default value
                        if (parameter.HasDefaultValue && !values.Any()) {
                            return (Parameter: parameter, Value: parameter.DefaultValue);
                        }
                        // Otherwise, grab the first value in the grouping. There should only be one, since the container is a Dictionary.
                        else {
                            return (Parameter: parameter, Value: values.FirstOrDefault());
                        }
                    });

                var injections = injectionPairs
                    // Filter out nulls, unless defined as a parameter's default value
                    .Where(pair => pair.Parameter.HasDefaultValue || pair.Value != null)
                    // Sift out the parameters
                    .Select(pair => pair.Value)
                    .ToArray();

                if (injections.Length == parameters.Length) {
                    inject.Invoke(target, injections);
                } else {
                    var missingTypes = parameters.Select(p => p.ParameterType).Except(applicableBindings.Select(p => p.Key));
                    Debug.LogWarning(
                        $"Failed to satisfy the dependencies for {inject.DeclaringType}:{inject}\n" +
                        $"Missing bindings:\n\t{string.Join("\n\t", missingTypes)}"
                    );
                }
            }
        }

        private GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent, params object[] additionalBindings) {
            var instance = GameObject.Instantiate(original, position, rotation, parent);
            instance.transform.ApplyBindings(additionalBindings);
            return instance;
        }
    }

    public delegate GameObject Instantiator(
        GameObject original,
        Vector3 position = new Vector3(),
        Quaternion rotation = new Quaternion(),
        Transform parent = null,
        params object[] additionalBindings
    );

    public static class InjectionExtensions {
        public static void ApplyBindings(this Transform transform, params object[] additionalBindings) {
            foreach (var xform in transform.BreadthFirstTraversal()) {
                var climb = xform;
                var stack = new List<IContext>();
                while (climb != null) {
                    // Traverse the hierarchy from bottom to top, while traversing each gameObject's contexts from top to bottom
                    // This lets multiple contexts on a single gameObject depend on each other in a predictable way
                    var contexts = climb.GetComponents<IContext>().Reverse();
                    foreach (var context in contexts) {
                        stack.Add(context);
                    }
                    climb = climb.parent;
                }

                var components = xform.gameObject
                    .GetComponents<Component>()
                    .SelectMany(c => Expand(c))
                    // Handle contexts first
                    .Select(c => (Component: c, Order: c is IContext ? 0 : 1))
                    .OrderBy(t => t.Order)
                    .Select(t => t.Component);

                foreach (var component in components) {
                    try {
                        var container = new DiContainer(additionalBindings);
                        for (int i = stack.Count - 1; i >= 0; i--) {
                            var context = stack[i];
                            if (component != context) {
                                context.InstallBindings(container);
                            }
                        }
                        container.Inject(component);
                    } catch (Exception error) {
                        Debug.LogException(error);
                    }
                }
            }
        }

        public static IEnumerable<object> Expand(this Component component) {
            yield return component;

            var animator = component as Animator;
            if (animator != null) {
                foreach (var behaviour in animator.GetBehaviours<StateMachineBehaviour>()) {
                    yield return behaviour;
                }
            }
        }
    }
}
