using System;
using System.Linq;

using UniRx;
using UniRx.Triggers;
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
            var syncFrames = observeState
                .Where(state => isActiveAndEnabled && state.IsClient())
                .Select(state => state.GetLatestPawnSyncFrame())
                .Where(frame => frame != null && frame.Value.PawnId == pawnId)
                .DistinctUntilChanged()
                .Select(frame => frame.Value);

            syncFrames
                .CatchIgnoreLog()
                .Subscribe(frame => {
                    animator.enabled = false;
                    animator.UseFrame(frame);
                    animator.enabled = true;
                })
                .AddTo(this);

            var interpolationFrames = 5;

            syncFrames
                .SelectMany(
                    frame => Observable.EveryFixedUpdate()
                        .Take(interpolationFrames)
                        .Select(frameCount => (frame, frameCount))
                )
                .CatchIgnoreLog()
                .Subscribe(args => {
                    var (frame, frameCount) = args;
                    motor.Teleport(
                        Vector3.Slerp(motor.TargetPosition, frame.Position, (float)frameCount / interpolationFrames)
                    );
                    orientation.TrackForward(
                        Vector3.Slerp(orientation.transform.forward, frame.Forward, (float)frameCount / interpolationFrames)
                    );
                })
                .AddTo(this);
        }

        private void Start() { }
    }
}