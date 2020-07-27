using System.Collections.Generic;
using System.Linq;

namespace Banchou.Board {
    public static class BoardSelectors {
        public static bool IsBoardLoading(this GameState state) {
            return state.Board.LoadingScenes.Any();
        }

        public static string GetLatestScene(this GameState state) {
            return state.Board.LatestScene;
        }

        public static IEnumerable<string> GetLoadedScenes(this GameState state) {
            return state.Board.LoadedScenes;
        }
    }
}