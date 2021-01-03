using System.Collections.Generic;

namespace Banchou.Stage {
    public class StageState {
        public string LatestScene = null;
        public HashSet<string> LoadingScenes = new HashSet<string>();
        public HashSet<string> LoadedScenes = new HashSet<string>();

        public StageState() { }
        public StageState(in StageState prev) {
            LatestScene = prev.LatestScene;
            LoadingScenes = prev.LoadingScenes;
            LoadedScenes = prev.LoadedScenes;
        }
    }
}