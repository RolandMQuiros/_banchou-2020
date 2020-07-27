using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using UniRx.Triggers;
using Redux;

namespace Banchou.Player.Part {
    public class PlayerInputDispatchers : MonoBehaviour {
        private PlayerId _playerId;
        private Dispatcher _dispatch;

        private PlayerActions _playerActions;
        private PlayerInputStreams _playerInputStreams;

        private Vector2 _moveInput;

        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            PlayerInputStreams playerInputStreams
        ) {
            _playerId = playerId;
            _dispatch = dispatch;

            _playerActions = playerActions;
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
                _dispatch(_playerActions.LockOn(_playerId));
            } else {
                _dispatch(_playerActions.LockOff(_playerId));
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_playerActions.LockOff(_playerId));
            }
        }
    }
}