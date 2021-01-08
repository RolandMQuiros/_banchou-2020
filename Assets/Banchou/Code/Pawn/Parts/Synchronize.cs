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
                    animator.UseFrame(frame);
                })
                .AddTo(this);
        }

        private void Start() { }
    }
}