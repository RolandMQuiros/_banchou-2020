using System.Linq;
using UnityEngine;

namespace Banchou.DependencyInjection {
    public class SceneInjector : MonoBehaviour {
        private void Awake() {
            var all = gameObject.scene.GetRootGameObjects()
                .Select(obj => obj.transform);

            // For every object in the scene, traverse up the hierarchy and aggregate the bindings
            // of every context along the way
            foreach (var xform in all) {
                xform.ApplyBindings();
            }
        }
    }
}