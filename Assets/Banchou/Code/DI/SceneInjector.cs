using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou {
    public class SceneInjector : MonoBehaviour {
        private void Awake() {
            var all = gameObject.scene.GetRootGameObjects()
                .SelectMany(obj => obj.transform.BreadthFirstTraversal());

            // For every object in the scene, traverse up the hierarchy and aggregate the bindings
            // of every context along the way
            foreach (var xform in all) {
                var climb = xform;
                var stack = new List<IContext>();
                while (climb != null) {
                    var contexts = climb.GetComponents<IContext>();
                    foreach (var context in contexts) {
                        stack.Add(context);
                    }
                    climb = climb.parent;
                }

                var components = xform.gameObject
                    .GetComponents<Component>()
                    .SelectMany(c => ExpandComponent(c));

                foreach (var component in components) {
                    try {
                        var container = new DiContainer();
                        for (int i = stack.Count - 1; i >= 0; i--) {
                            var context = stack[i];
                            context.InstallBindings(container);
                        }
                        container.Inject(component);
                    } catch (Exception error) {
                        Debug.LogException(error);
                    }
                }
            }
        }

        private IEnumerable<object> ExpandComponent(Component component) {
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