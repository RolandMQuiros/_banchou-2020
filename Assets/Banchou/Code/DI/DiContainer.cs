using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou {
    public sealed class DiContainer {
        private Dictionary<Type, object> _bindings = new Dictionary<Type, object>();

        public DiContainer() {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);
        }

        public DiContainer(in DiContainer prev) {
            Bind<DiContainer>(this);
            Bind<Instantiator>(Instantiate);
        }

        public void Bind<T>(T instance) {
            _bindings[typeof(T)] = instance;
        }

        public void Inject(object target) {
            if (target.GetType() == typeof(DiContainer)) {
                throw new Exception("Cannot inject into a DiContainer");
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
                        inner: _bindings,
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
                    var missingTypes = parameters.Select(p => p.ParameterType).Except(_bindings.Keys);
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
