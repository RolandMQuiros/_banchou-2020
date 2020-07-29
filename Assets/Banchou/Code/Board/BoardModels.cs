using System.Collections.Generic;

namespace Banchou.Board {
    public class BoardState {
        public string LatestScene = null;
        public HashSet<string> LoadingScenes = new HashSet<string>();
        public HashSet<string> LoadedScenes = new HashSet<string>();

        public BoardState() { }
        public BoardState(in BoardState prev) {
            LatestScene = prev.LatestScene;
            LoadingScenes = prev.LoadingScenes;
            LoadedScenes = prev.LoadedScenes;
        }
    }
}