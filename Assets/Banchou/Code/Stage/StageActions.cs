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
        private GetServerTime _getServerTime;
        public StageActions(GetServerTime getServerTime) {
            _getServerTime = getServerTime;
        }

        public StateAction.AddScene AddScene(string sceneName, float? when = null) => new StateAction.AddScene {
            Scene = sceneName,
            When = when ?? _getServerTime()
        };

        public StateAction.SetScene SetScene(string sceneName, float? when = null) => new StateAction.SetScene {
            Scene = sceneName,
            When = when ?? _getServerTime()
        };

        public StateAction.SceneLoaded SceneLoaded(string sceneName, float? when = null) => new StateAction.SceneLoaded {
            Scene = sceneName,
            When = when ?? _getServerTime()
        };
    }
}