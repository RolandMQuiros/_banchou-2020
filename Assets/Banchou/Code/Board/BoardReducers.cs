using System.Collections.Generic;

namespace Banchou.Board {
    public static class BoardReducers {
        public static BoardState Reduce(in BoardState prev, in object action) {
            if (action is StateAction.AddScene add) {
                if (!prev.LoadingScenes.Contains(add.Scene) && !prev.LoadedScenes.Contains(add.Scene)) {
                    var loading = new HashSet<string>(prev.LoadingScenes);
                    loading.Add(add.Scene);
                    return new BoardState(prev) { LoadingScenes = loading };
                }
            }

            if (action is StateAction.SetScene set) {
                if (!prev.LoadingScenes.Contains(set.Scene) && !prev.LoadedScenes.Contains(set.Scene)) {
                    var loading = new HashSet<string>(prev.LoadingScenes);
                    loading.Add(set.Scene);

                    return new BoardState(prev) {
                        LoadingScenes = loading,
                        LoadedScenes = new HashSet<string>()
                    };
                }
            }

            if (action is StateAction.SceneLoaded done) {
                var loading = new HashSet<string>(prev.LoadingScenes);
                loading.Remove(done.Scene);

                var loaded = new HashSet<string>(prev.LoadedScenes);
                loaded.Add(done.Scene);

                return new BoardState(prev) {
                    LatestScene = done.Scene,
                    LoadedScenes = loaded
                };
            }

            return prev;
        }
    }
}