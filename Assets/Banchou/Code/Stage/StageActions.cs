using Banchou.Network;

namespace Banchou.Stage {
    namespace StateAction {
        public struct AddScene {
            public string Scene;
            public float When;
        }

        public struct SetScene {
            public string Scene;
            public float When;
        }

        public struct SceneLoaded {
            public string Scene;
            public float When;
        }
    }

    public class StageActions {
        private GetTime _getTime;
        public StageActions(GetTime getTime) {
            _getTime = getTime;
        }

        public StateAction.AddScene AddScene(string sceneName, float? when = null) => new StateAction.AddScene {
            Scene = sceneName,
            When = when ?? _getTime()
        };

        public StateAction.SetScene SetScene(string sceneName, float? when = null) => new StateAction.SetScene {
            Scene = sceneName,
            When = when ?? _getTime()
        };

        public StateAction.SceneLoaded SceneLoaded(string sceneName, float? when = null) => new StateAction.SceneLoaded {
            Scene = sceneName,
            When = when ?? _getTime()
        };
    }
}