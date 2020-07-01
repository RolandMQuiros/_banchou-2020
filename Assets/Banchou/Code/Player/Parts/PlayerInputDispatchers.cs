using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using Redux;

using Banchou.Pawn;
using Banchou.Combatant;

namespace Banchou.Player.Part {
    public class PlayerInputDispatchers : MonoBehaviour {
        private PlayerId _playerId;
        private PawnId _pawnId;
        private Dispatcher _dispatch;

        private PlayerActions _playerActions;
        private CombatantActions _combatantActions;

        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            CombatantActions combatantActions
        ) {
            _playerId = playerId;
            _dispatch = dispatch;

            _playerActions = playerActions;
            _combatantActions = combatantActions;

            observeState
                .Select(state => state.GetPlayerPawn(playerId))
                .DistinctUntilChanged()
                .Subscribe(pawn => {
                    _pawnId = pawn;
                })
                .AddTo(this);
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
                _dispatch(_combatantActions.PushCommand(_pawnId, Command.LightAttack));
            }
        }

        public void DispatchHeavyAttack(InputAction.CallbackContext callbackContext) {
             if (callbackContext.performed) {
                _dispatch(_combatantActions.PushCommand(_pawnId, Command.HeavyAttack));
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