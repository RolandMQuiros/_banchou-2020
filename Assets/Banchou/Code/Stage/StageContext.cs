using UnityEngine;
using Banchou.DependencyInjection;

namespace Banchou.Stage {
    public class StageContext : MonoBehaviour, IContext {
        public void InstallBindings(DiContainer container) {
            container.Bind(new StageActions());
        }
    }
}