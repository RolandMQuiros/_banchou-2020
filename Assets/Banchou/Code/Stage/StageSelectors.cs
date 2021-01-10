using System.Collections.Generic;
using System.Linq;

namespace Banchou.Stage {
    public static class StageSelectors {
        public static bool AreScenesLoading(this GameState state) {
            return state.Stage.LoadingScenes.Any();
        }

        public static IEnumerable<string> GetLoadingScenes(this GameState state) {
            return state.Stage.LoadingScenes;
        }

        public static IEnumerable<string> GetLoadedScenes(this GameState state) {
            return state.Stage.LoadedScenes;
        }
    }
}