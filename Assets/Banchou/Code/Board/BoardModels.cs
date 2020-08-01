using MessagePack;
using System.Collections.Generic;

namespace Banchou.Board {
    [MessagePackObject]
    public class BoardState {
        [Key(0)] public string LatestScene = null;
        [Key(1)] public HashSet<string> LoadingScenes = new HashSet<string>();
        [Key(2)] public HashSet<string> LoadedScenes = new HashSet<string>();

        public BoardState() { }
        public BoardState(in BoardState prev) {
            LatestScene = prev.LatestScene;
            LoadingScenes = prev.LoadingScenes;
            LoadedScenes = prev.LoadedScenes;
        }
    }
}