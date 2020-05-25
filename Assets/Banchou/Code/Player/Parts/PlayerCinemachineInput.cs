using System;

using UnityEngine;
using Cinemachine;

using UniRx;

namespace Banchou.Player.Part {
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class PlayerCinemachineInput : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState
        ) {
            var x = 0f;
            var y = 0f;

            CinemachineCore.GetInputAxis = axisName => {
                if (axisName == "Mouse X") { return x; }
                if (axisName == "Mouse Y") { return y; }
                return 0f;
            };

            observeState
                .Select(state => state.GetPlayer(playerId))
                .Where(player => player?.Source == InputSource.LocalSingle || player?.Source == InputSource.LocalMulti)
                .Select(player => player.InputLook)
                .Subscribe(direction => {
                    x = direction.x;
                    y = direction.y;
                })
                .AddTo(this);
        }
    }
}