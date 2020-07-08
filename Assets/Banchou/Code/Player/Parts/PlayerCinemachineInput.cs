using UnityEngine;
using Cinemachine;

using UniRx;
using UniRx.Triggers;

namespace Banchou.Player.Part {
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class PlayerCinemachineInput : MonoBehaviour {
        public void Construct(ObservePlayerLook observePlayerLook) {
            var x = 0f;
            var y = 0f;

            CinemachineCore.GetInputAxis = axisName => {
                if (axisName == "Mouse X") { return x; }
                if (axisName == "Mouse Y") { return y; }
                return 0f;
            };

            this.FixedUpdateAsObservable()
                .WithLatestFrom(observePlayerLook(), (_, look) => look)
                .Subscribe(direction => {
                    x = direction.x;
                    y = direction.y;
                })
                .AddTo(this);
        }
    }
}