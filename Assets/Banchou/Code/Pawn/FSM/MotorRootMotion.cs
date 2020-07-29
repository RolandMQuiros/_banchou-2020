using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Banchou.Pawn.FSM {
    public class MotorRootMotion : FSMBehaviour {
        [SerializeField] private bool _rootPosition = true;
        [SerializeField] private bool _rootOrientation = false;

        public void Construct(
            Animator animator,
            Part.IMotor motor,
            Part.Orientation orientation
        ) {
            animator.OnAnimatorMoveAsObservable()
                .Where(_ => IsStateActive)
                .Subscribe(_ => {
                    if (_rootOrientation) {
                        orientation.transform.rotation *= animator.deltaRotation;
                    }

                    if (_rootPosition) {
                        motor.Move(animator.deltaPosition);
                    }
                })
                .AddTo(this);
        }
    }
}