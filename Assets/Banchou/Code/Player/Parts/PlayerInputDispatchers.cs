using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;

using Banchou.Network;

namespace Banchou.Player.Part {
    public class PlayerInputDispatchers : MonoBehaviour {
        private PlayerId _playerId;
        private PlayerInputStreams _playerInputStreams;
        private Vector2 _moveInput;
        private GetServerTime _getTime;

        public void Construct(
            PlayerId playerId,
            PlayerInputStreams playerInputStreams,
            GetServerTime getServerTime
        ) {
            _playerId = playerId;
            _playerInputStreams = playerInputStreams;
            _getTime = getServerTime;

            this.FixedUpdateAsObservable()
                .Select(_ => _moveInput.CameraPlaneProject())
                .DistinctUntilChanged()
                .Subscribe(direction => {
                    _playerInputStreams.PushMove(_playerId, direction, _getTime());
                })
                .AddTo(this);
        }

        public void DispatchMovement(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _moveInput = direction;
        }

        public void DispatchLook(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _playerInputStreams.PushLook(_playerId, direction, _getTime());
        }

        public void DispatchLightAttack(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LightAttack, _getTime());
            }
        }

        public void DispatchHeavyAttack(InputAction.CallbackContext callbackContext) {
             if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.HeavyAttack, _getTime());
            }
        }

        public void DispatchLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOn, _getTime());
            } else {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getTime());
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getTime());
            }
        }
    }
}