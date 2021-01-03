using System;
using System.Linq;

using Redux;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Banchou.Stage {
    public class SceneLoader : MonoBehaviour {
        public void Construct(
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            StageActions stageActions
        ) {
            // Load scenes
            observeState.Select(state => state.GetLatestScene())
                .DistinctUntilChanged()
                .Where(scene => scene != null)
                .SelectMany(
                    scene => SceneManager
                        .LoadSceneAsync(scene, LoadSceneMode.Additive)
                        .AsObservable()
                        .Select(_ => scene)
                        .Last()
                )
                .Subscribe(scene => { dispatch(stageActions.SceneLoaded(scene)); })
                .AddTo(this);

            // Unload scenes
            observeState.Select(state => state.GetLoadedScenes())
                .DistinctUntilChanged()
                .Pairwise()
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .Subscribe(scene => SceneManager.UnloadSceneAsync(scene))
                .AddTo(this);
        }
    }
}