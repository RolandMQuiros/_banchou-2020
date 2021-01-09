using UnityEngine;
using Banchou.DependencyInjection;
using Banchou.Network;

namespace Banchou.Stage {
    public class StageContext : MonoBehaviour, IContext {
        private StageActions _stageActions;
        public void Construct(GetTime getTime) {
            _stageActions = new StageActions(getTime);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind(_stageActions);
        }
    }
}