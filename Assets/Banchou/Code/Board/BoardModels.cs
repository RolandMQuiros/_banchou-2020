using System.Collections.Generic;

namespace Banchou.Board {
    public class BoardState {
        public string LatestScene = null;
        public HashSet<string> LoadingScenes;
        public HashSet<string> LoadedScenes;

        public BoardState() { }
        public BoardState(in BoardState prev) {
            LatestScene = prev.LatestScene;
            LoadingScenes = prev.LoadingScenes;
            LoadedScenes = prev.LoadedScenes;
        }
    }
}