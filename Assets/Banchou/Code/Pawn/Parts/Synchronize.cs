using System;
using System.Linq;
using UniRx;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class Synchronize : MonoBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            IMotor motor,
            Orientation orientation,
            Animator animator
        ) {
            var triggerKeys = animator.parameters
                .Where(p => p.type == AnimatorControllerParameterType.Trigger)
                .Select(p => p.nameHash);

            observeState
                .Where(state => isActiveAndEnabled && state.IsClient())
                .Select(state => state.GetLatestPawnSyncFrame())
                .Where(frame => frame != null && frame.Value.PawnId == pawnId)
                .DistinctUntilChanged()
                .Select(frame => frame.Value)
                .CatchIgnoreLog()
                .Subscribe(frame => {
                    motor.Teleport(frame.Position);
                    orientation.TrackForward(frame.Forward);

                    for (int layer = 0; layer < animator.layerCount; layer++) {
                        animator.Play(frame.StateHashes[layer], layer, frame.NormalizedTimes[layer]);
                    }

                    // Set animator parameters
                    foreach (var param in frame.Floats) {
                        animator.SetFloat(param.Key, param.Value);
                    }

                    foreach (var param in frame.Ints) {
                        animator.SetInteger(param.Key, param.Value);
                    }

                    foreach (var param in frame.Bools) {
                        animator.SetBool(param.Key, param.Value);
                    }

                    // Reset triggers
                    foreach (var param in triggerKeys) {
                        animator.ResetTrigger(param);
                    }
                })
                .AddTo(this);
        }

        private void Start() { }
    }
}