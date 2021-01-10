using System.Linq;
using System.Collections.Generic;

namespace Banchou.Stage {
    public class StageState {
        public IEnumerable<string> LoadingScenes = Enumerable.Empty<string>();
        public IEnumerable<string> LoadedScenes = Enumerable.Empty<string>();
        public float LastUpdated = 0f;
        public StageState() { }
        public StageState(in StageState prev) {
            LoadingScenes = prev.LoadingScenes;
            LoadedScenes = prev.LoadedScenes;
            LastUpdated = prev.LastUpdated;
        }
    }
}