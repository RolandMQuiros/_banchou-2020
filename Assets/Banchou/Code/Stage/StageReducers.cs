using System.Collections.Generic;

namespace Banchou.Stage {
    public static class StageReducers {
        public static StageState Reduce(in StageState prev, in object action) {
            if (action is StateAction.AddScene add) {
                if (!prev.LoadingScenes.Contains(add.Scene) && !prev.LoadedScenes.Contains(add.Scene)) {
                    var loading = new HashSet<string>(prev.LoadingScenes);
                    loading.Add(add.Scene);
                    return new StageState(prev) {
                        LatestScene = add.Scene,
                        LoadingScenes = loading,
                        LastUpdated = add.When
                    };
                }
            }

            if (action is StateAction.SetScene set) {
                if (!prev.LoadingScenes.Contains(set.Scene) && !prev.LoadedScenes.Contains(set.Scene)) {
                    var loading = new HashSet<string>(prev.LoadingScenes);
                    loading.Add(set.Scene);

                    return new StageState(prev) {
                        LatestScene = set.Scene,
                        LoadingScenes = loading,
                        LoadedScenes = new HashSet<string>(),
                        LastUpdated = set.When
                    };
                }
            }

            if (action is StateAction.SceneLoaded done) {
                var loading = new HashSet<string>(prev.LoadingScenes);
                loading.Remove(done.Scene);

                var loaded = new HashSet<string>(prev.LoadedScenes);
                loaded.Add(done.Scene);

                return new StageState(prev) {
                    LoadedScenes = loaded,
                    LoadingScenes = loading,
                    LastUpdated = done.When
                };
            }

            if (action is Network.StateAction.SyncGameState sync) {
                return sync.GameState.Stage;
            }

            return prev;
        }
    }
}