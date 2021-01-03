using System.Collections.Generic;
using System.Linq;

namespace Banchou.Stage {
    public static class StageSelectors {
        public static bool IsStageing(this GameState state) {
            return state.Stage.LoadingScenes.Any();
        }

        public static string GetLatestScene(this GameState state) {
            return state.Stage.LatestScene;
        }

        public static IEnumerable<string> GetLoadedScenes(this GameState state) {
            return state.Stage.LoadedScenes;
        }
    }
}