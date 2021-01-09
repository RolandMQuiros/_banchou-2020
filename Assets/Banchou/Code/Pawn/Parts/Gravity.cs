using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Banchou.Pawn.Part {
    public class Gravity : MonoBehaviour {
        public void Construct(
            IMotor motor,
            CharacterController controller,
            GetDeltaTime getDeltaTime
        ) {
            var speed = 0f;

            this.FixedUpdateAsObservable()
                .Subscribe(_ => {
                    if (controller.isGrounded) {
                        speed = 0f;
                    } else {
                        speed += 100f * getDeltaTime() * getDeltaTime();
                        motor.Move(Vector3.down * speed);
                    }
                })
                .AddTo(this);
        }
    }
}