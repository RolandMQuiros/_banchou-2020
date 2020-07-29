using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

namespace Banchou.Player.Part {
    public class PlayerInputDispatchers : MonoBehaviour {
        private PlayerId _playerId;
        private PlayerInputStreams _playerInputStreams;
        private Vector2 _moveInput;

        public void Construct(
            PlayerId playerId,
            PlayerInputStreams playerInputStreams
        ) {
            _playerId = playerId;
            _playerInputStreams = playerInputStreams;

            this.FixedUpdateAsObservable()
                .Select(_ => _moveInput.CameraPlaneProject())
                .DistinctUntilChanged()
                .Subscribe(direction => {
                    _playerInputStreams.PushMove(_playerId, direction, Time.fixedUnscaledTime);
                })
                .AddTo(this);
        }

        public void DispatchMovement(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _moveInput = direction;
        }

        public void DispatchLook(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _playerInputStreams.PushLook(_playerId, direction, Time.fixedUnscaledTime);
        }

        public void DispatchLightAttack(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LightAttack, Time.fixedUnscaledTime);
            }
        }

        public void DispatchHeavyAttack(InputAction.CallbackContext callbackContext) {
             if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.HeavyAttack, Time.fixedUnscaledTime);
            }
        }

        public void DispatchLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOn, Time.fixedUnscaledTime);
            } else {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, Time.fixedUnscaledTime);
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, Time.fixedUnscaledTime);
            }
        }
    }
}