using System.Linq;
using System.Collections.Generic;

namespace Banchou.Stage {
    public static class StageReducers {
        public static StageState Reduce(in StageState prev, in object action) {
            if (action is StateAction.AddScene add) {
                if (!prev.LoadingScenes.Contains(add.Scene) && !prev.LoadedScenes.Contains(add.Scene)) {
                    return new StageState(prev) {
                        LoadingScenes = prev.LoadingScenes.Append(add.Scene).Distinct(),
                        LastUpdated = add.When
                    };
                }
            }

            if (action is StateAction.SetScene set) {
                if (!prev.LoadingScenes.Contains(set.Scene) && !prev.LoadedScenes.Contains(set.Scene)) {
                    return new StageState(prev) {
                        LoadingScenes = prev.LoadingScenes.Append(set.Scene).Distinct(),
                        LoadedScenes = Enumerable.Empty<string>(),
                        LastUpdated = set.When
                    };
                }
            }

            if (action is StateAction.SceneLoaded done) {
                return new StageState(prev) {
                    LoadedScenes = prev.LoadedScenes.Append(done.Scene),
                    LoadingScenes = prev.LoadingScenes.Where(s => s != done.Scene),
                    LastUpdated = done.When
                };
            }

            if (action is Network.StateAction.SyncGameState sync) {
                var syncStage = sync.GameState.Stage;
                return new StageState(prev) {
                    LoadingScenes = prev.LoadingScenes
                        .Concat(syncStage.LoadedScenes)
                        .Concat(syncStage.LoadingScenes)
                        .Distinct()
                        .Except(prev.LoadedScenes),
                    LastUpdated = syncStage.LastUpdated
                };
            }

            return prev;
        }
    }
}