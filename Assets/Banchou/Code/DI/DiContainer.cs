using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou {
    public sealed class DiContainer {
        private class Binding {
            public object Instance = null;
            public Func<Type, bool> Condition = null;
        }

        private Dictionary<Type, Binding> _bindings = new Dictionary<Type, Binding>();

        public DiContainer() {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);
        }

        public DiContainer(in DiContainer prev) {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);
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
                .Cast<MethodBase>()
                .Concat(
                    targetType
                        .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                        .Where(ctor => ctor.GetParameters().Any())
                );

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

        private GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent) {
            var instance = GameObject.Instantiate(original, position, rotation, parent);
            Inject(instance);
            return instance;
        }
    }

    public delegate GameObject Instantiator(GameObject original, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion(), Transform parent = null);
}
