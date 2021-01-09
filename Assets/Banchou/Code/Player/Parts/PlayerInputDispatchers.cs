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
        private GetTime _getTime;

        public void Construct(
            PlayerId playerId,
            PlayerInputStreams playerInputStreams,
            GetTime getTime
        ) {
            _playerId = playerId;
            _playerInputStreams = playerInputStreams;
            _getTime = getTime;

            this.FixedUpdateAsObservable()
                .Select(_ => _moveInput.CameraPlaneProject())
                // Quantize the world direction so we don't emit changes every frame
                // This eases the load on Rollback
                .Select(direction => Snapping.Snap(direction, new Vector3(0.25f, 0.25f, 0.25f), SnapAxis.All))
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

        private bool _lockOnDown = false;
        public void DispatchLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _lockOnDown = !_lockOnDown;

                if (_lockOnDown) {
                    _playerInputStreams.PushCommand(_playerId, InputCommand.LockOn, _getTime());
                } else {
                    _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getTime());
                }
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getTime());
            }
        }
    }
}