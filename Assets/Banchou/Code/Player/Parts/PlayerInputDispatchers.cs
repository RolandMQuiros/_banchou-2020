using UnityEngine;
using UnityEngine.InputSystem;
using Redux;

using Banchou.Player.Activation;
using Banchou.Player.Targeting;

namespace Banchou.Player.Part {
    public class PlayerInputDispatchers : MonoBehaviour {
        private PlayerId _playerId;
        private GetState _getState;
        private Dispatcher _dispatch;

        private PlayerActions _playerActions;
        private TargetingActions _targetingActions;
        private ActivationActions _activationActions;

        public void Construct(
            PlayerId playerId,
            GetState getState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            TargetingActions targetingActions,
            ActivationActions activationActions
        ) {
            _playerId = playerId;
            _getState = getState;
            _dispatch = dispatch;

            _playerActions = playerActions;
            _targetingActions = targetingActions;
            _activationActions = activationActions;
        }

        public void DispatchMovement(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _dispatch(_playerActions.Move(_playerId, direction));
        }

        public void DispatchLook(InputAction.CallbackContext callbackContext) {
            var direction = callbackContext.ReadValue<Vector2>();
            _dispatch(_playerActions.Look(_playerId, direction));
        }

        public void DispatchLightAttack(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_playerActions.PushCommand(_playerId, Command.LightAttack, Time.unscaledTime));
            }
        }

        public void DispatchHeavyAttack(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_playerActions.PushCommand(_playerId, Command.HeavyAttack, Time.unscaledTime));
            }
        }

        public void DispatchLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_targetingActions.ToggleLockOn(_playerId));
            }
        }

        public void DispatchLockOff(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_targetingActions.LockOff(_playerId));
            }
        }

        public void DispatchToggleLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_targetingActions.ToggleLockOn(_playerId));
            }
        }

        public void DispatchNextLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_targetingActions.NextLockOn(_playerId));
            }
        }

        public void DispatchPreviousLockOn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_targetingActions.PreviousLockOn(_playerId));
            }
        }

        public void DispatchActivateLeftPawn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ActivateLeftPawn(_playerId));
            }
        }

        public void DispatchToggleLeftPawn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ToggleLeftPawn(_playerId));
            }
        }


        public void DispatchActivateRightPawn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ActivateRightPawn(_playerId));
            }
        }

        public void DispatchToggleRightPawn(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ToggleRightPawn(_playerId));
            }
        }

        public void DispatchActivateAllPawns(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ActivateAllPawns(_playerId));
            }
        }

        public void DispatchResetPawns(InputAction.CallbackContext callbackContext) {
            if (callbackContext.performed) {
                _dispatch(_activationActions.ResetActivations(_playerId));
            }
        }
    }
}