using Redux;

using Banchou.Pawn;
using PlayerAction = Banchou.Player.StateAction.PlayerAction;

namespace Banchou.Player.Activation {
    namespace StateAction {
        public class ActivateLeftPawn : PlayerAction { }
        public class DeactivateLeftPawn : PlayerAction { }
        public class ActivateRightPawn : PlayerAction { }
        public class DeactivateRightPawn : PlayerAction { }
        public class ActivateAllPawns : PlayerAction { }
        public class ResetActivations : PlayerAction { }
    }

    public class ActivationActions {
        public StateAction.ActivateLeftPawn ActivateLeftPawn(PlayerId playerId) {
            return new StateAction.ActivateLeftPawn {
                PlayerId = playerId
            };
        }

        public StateAction.DeactivateLeftPawn DeactivateLeftPawn(PlayerId playerId) {
            return new StateAction.DeactivateLeftPawn {
                PlayerId = playerId
            };
        }

        public ActionsCreator<GameState> ToggleLeftPawn(PlayerId playerId) => (dispatch, getState) => {
            if (!getState().IsPlayerLeftPawnActivated(playerId)) {
                dispatch(ActivateLeftPawn(playerId));
            } else {
                dispatch(ResetActivations(playerId));
            }
        };

        public StateAction.ActivateRightPawn ActivateRightPawn(PlayerId playerId) {
            return new StateAction.ActivateRightPawn {
                PlayerId = playerId
            };
        }

        public StateAction.DeactivateRightPawn DeactivateRightPawn(PlayerId playerId) {
            return new StateAction.DeactivateRightPawn {
                PlayerId = playerId
            };
        }

        public ActionsCreator<GameState> ToggleRightPawn(PlayerId playerId) => (dispatch, getState) => {
            if (!getState().IsPlayerRightPawnActivated(playerId)) {
                dispatch(ActivateRightPawn(playerId));
            } else {
                dispatch(ResetActivations(playerId));
            }
        };

        public StateAction.ActivateAllPawns ActivateAllPawns(PlayerId playerId) {
            return new StateAction.ActivateAllPawns {
                PlayerId = playerId
            };
        }

        public StateAction.ResetActivations ResetActivations(PlayerId playerId) {
            return new StateAction.ResetActivations {
                PlayerId = playerId
            };
        }
    }
}