using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Redux;

using Banchou.Pawn;
using PlayerAction = Banchou.Player.StateAction.PlayerAction;

namespace Banchou.Player.Targeting {
    namespace StateAction {
        public class AddTarget : PlayerAction {
            public PawnId Target;
        }

        public class RemoveTarget : PlayerAction {
            public PawnId Target;
        }

        public class LockOn : PlayerAction {
            public PawnId To;
        }

        public class NextLockOn : PlayerAction {
            public PawnId To;
        }

        public class PreviousLockOn : PlayerAction {
            public PawnId To;
        }

        public class LockOff : PlayerAction { }
    }

    public class TargetingActions {
        private IPawnInstances _pawnInstances;
        public TargetingActions(IPawnInstances pawnInstances) {
            _pawnInstances = pawnInstances;
        }

        public StateAction.AddTarget AddTarget(PlayerId playerId, PawnId target) {
            return new StateAction.AddTarget {
                PlayerId = playerId,
                Target = target
            };
        }

        public StateAction.RemoveTarget RemoveTarget(PlayerId playerId, PawnId target) {
            return new StateAction.RemoveTarget {
                PlayerId = playerId,
                Target = target
            };
        }

        public ActionsCreator<GameState> LockOn(PlayerId playerId) => (dispatch, getState) => {
            var player = getState().GetPlayer(playerId);
            var target = ChooseTarget(player, player.Targets, true);

            if (target != PawnId.Empty) {
                dispatch(new StateAction.LockOn {
                    PlayerId = playerId,
                    To = target
                });
            }
        };

        public StateAction.LockOff LockOff(PlayerId playerId) {
            return new StateAction.LockOff {
                PlayerId = playerId
            };
        }

        public ActionsCreator<GameState> ToggleLockOn(PlayerId playerId) => (dispatch, getState) => {
            if (getState().GetPlayerLockOnTarget(playerId) == PawnId.Empty) {
                dispatch(LockOn(playerId));
            } else {
                dispatch(LockOff(playerId));
            }
        };

        public ActionsCreator<GameState> NextLockOn(PlayerId playerId) => (dispatch, getState) => {
            var player = getState().GetPlayer(playerId);
            if (player.LockOnTarget != PawnId.Empty) {
                var target = ChooseTarget(player, player.LockOnFuture, false);
                if (target != PawnId.Empty) {
                    dispatch(new StateAction.NextLockOn {
                        PlayerId = playerId,
                        To = target
                    });
                }
            }
        };

        public ActionsCreator<GameState> PreviousLockOn(PlayerId playerId) => (dispatch, getState) => {
            var player = getState().GetPlayer(playerId);
            if (player.LockOnTarget != PawnId.Empty) {
                var target = ChooseTarget(player, player.LockOnHistory, false);
                if (target != PawnId.Empty) {
                    dispatch(new StateAction.PreviousLockOn {
                        PlayerId = playerId,
                        To = target
                    });
                }
            }
        };

        private PawnId ChooseTarget(PlayerState player, IEnumerable<PawnId> from, bool filterHidden) {
            var selected = player.SelectedPawns.Select(id => _pawnInstances.Get(id));
            var targets = from.Select(id => _pawnInstances.Get(id));
            var camera = Camera.main.transform;

            var centroid = selected.Select(t => t.Position).Centroid();
            return targets.PrioritizeTargets(camera, filterHidden)
                .Select(instance => instance.PawnId)
                .FirstOrDefault();
        }
    }
}