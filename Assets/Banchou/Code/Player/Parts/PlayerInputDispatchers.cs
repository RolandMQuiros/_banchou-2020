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
        private GetServerTime _getServerTime;

        public void Construct(
            PlayerId playerId,
            PlayerInputStreams playerInputStreams,
            GetServerTime getServerTime
        ) {
            _playerId = playerId;
            _playerInputStreams = playerInputStreams;
            _getServerTime = getServerTime;

            this.FixedUpdateAsObservable()
                .Select(_ => _moveInput.CameraPlaneProject())
                // Quantize the world direction so we don't emit changes every frame
                // This eases the load on Rollback
                .Select(direction => Snapping.Snap(direction, new Vector3(0.25f, 0.25f, 0.25f), SnapAxis.All))
                .DistinctUntilChanged()
                .Subscribe(direction => {
                    _playerInputStreams.PushMove(_playerId, direction, _getServerTime());
                })
                .AddTo(this);
        }

        public void DispatchMovement(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _moveInput = direction;
        }

        public void DispatchLook(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _playerInputStreams.PushLook(_playerId, direction, _getServerTime());
        }

        public void DispatchLightAttack(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LightAttack, _getServerTime());
            }
        }

        public void DispatchHeavyAttack(InputAction.CallbackContext callbackContext) {
             if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.HeavyAttack, _getServerTime());
            }
        }

        private bool _lockOnDown = false;
        public void DispatchLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _lockOnDown = !_lockOnDown;

                if (_lockOnDown) {
                    _playerInputStreams.PushCommand(_playerId, InputCommand.LockOn, _getServerTime());
                } else {
                    _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getServerTime());
                }
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _playerInputStreams.PushCommand(_playerId, InputCommand.LockOff, _getServerTime());
            }
        }
    }
}