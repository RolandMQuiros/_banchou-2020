namespace Banchou.Stage {
    namespace StateAction {
        public struct AddScene {
            public string Scene;
        }

        public struct SetScene {
            public string Scene;
        }

        public struct SceneLoaded {
            public string Scene;
        }
    }

    public class StageActions {

        public StateAction.AddScene AddScene(string sceneName) => new StateAction.AddScene {
            Scene = sceneName
        };

        public StateAction.SetScene SetScene(string sceneName) => new StateAction.SetScene {
            Scene = sceneName
        };

        public StateAction.SceneLoaded SceneLoaded(string sceneName) => new StateAction.SceneLoaded {
            Scene = sceneName
        };
    }
}