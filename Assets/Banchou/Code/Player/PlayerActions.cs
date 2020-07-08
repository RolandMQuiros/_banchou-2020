using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Redux;
using Redux.DevTools;

using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;

namespace Banchou.Player {
    namespace StateAction {
        public class PlayerAction {
            public PlayerId PlayerId;
        }

        public class AddPlayer {
            public PlayerId PlayerId;
            public InputSource Source;
        }

        public class RemovePlayer {
            public PlayerId PlayerId;
        }

        public class AttachPlayerToPawn : PlayerAction {
            public PawnId PawnId;
        }

        public class DetachPlayerFromPawn : PlayerAction { }

        public class PlayerMove : PlayerAction, ICollapsibleAction {
            public Vector2 Direction;
            public object Collapse(in object next) {
                var nextMove = next as PlayerMove;
                if (nextMove != null) {
                    Direction += nextMove.Direction;
                }
                return this;
            }
        }

        public class PlayerLook : PlayerAction, ICollapsibleAction {
            public Vector2 Direction;
            public object Collapse(in object next) {
                var nextLook = next as PlayerLook;
                if (nextLook != null) {
                    Direction += nextLook.Direction;
                }
                return this;
            }
        }

        public class AddPlayerTarget : PlayerAction {
            public PawnId Target;
        }

        public class RemovePlayerTarget : PlayerAction {
            public PawnId Target;
        }
    }

    public class PlayerActions {
        private IPawnInstances _pawnInstances;

        private MobActions _mobActions;
        private CombatantActions _combatantActions;

        public void Construct(
            IPawnInstances pawnInstances,
            MobActions mobActions,
            CombatantActions combatantActions
        ) {
            _pawnInstances = pawnInstances;
            _mobActions = mobActions;
            _combatantActions = combatantActions;
        }

        public StateAction.AddPlayer Add(PlayerId playerId, InputSource source) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                Source = source
            };
        }

        public StateAction.RemovePlayer Remove(PlayerId playerId) {
            return new StateAction.RemovePlayer {
                PlayerId = playerId
            };
        }

        public StateAction.AttachPlayerToPawn Attach(PlayerId playerId, PawnId pawnId) {
            return new StateAction.AttachPlayerToPawn {
                PlayerId = playerId,
                PawnId = pawnId
            };
        }

        public StateAction.DetachPlayerFromPawn Detach(PlayerId playerId) {
            return new StateAction.DetachPlayerFromPawn {
                PlayerId = playerId
            };
        }

        public StateAction.PlayerMove Move(PlayerId playerId, Vector3 direction) {
            return new StateAction.PlayerMove {
                PlayerId = playerId,
                Direction = direction
            };
        }

        public StateAction.PlayerLook Look(PlayerId playerId, Vector2 direction) {
            return new StateAction.PlayerLook {
                PlayerId = playerId,
                Direction = direction
            };
        }

        public StateAction.AddPlayerTarget AddTarget(PlayerId playerId, PawnId target) {
            return new StateAction.AddPlayerTarget {
                PlayerId = playerId,
                Target = target
            };
        }

        public ActionsCreator<GameState> RemoveTarget(PlayerId playerId, PawnId target) => (dispatch, getState) => {
            dispatch(new StateAction.RemovePlayerTarget {
                PlayerId = playerId,
                Target = target
            });

            var pawn = getState().GetPlayerPawn(playerId);
            if (getState().GetCombatantLockOnTarget(pawn) == target) {
                dispatch(_combatantActions.LockOff(pawn));
            }
        };

        public ActionsCreator<GameState> LockOn(PlayerId playerId) => (dispatch, getState) => {
            var player = getState().GetPlayer(playerId);
            var pawn = getState().GetPlayerPawn(playerId);

            if (player != null && pawn != PawnId.Empty) {
                var to = ChooseTarget(pawn, getState().GetPlayerTargets(playerId));

                dispatch(_combatantActions.LockOn(
                    combatantId: pawn,
                    to: to
                ));
            }
        };

        public ActionsCreator<GameState> LockOff(PlayerId playerId) => (dispatch, getState) => {
            var pawn = getState().GetPlayerPawn(playerId);
            if (pawn != PawnId.Empty) {
                dispatch(_combatantActions.LockOff(pawn));
            }
        };

        public ActionsCreator<GameState> ToggleLockOn(PlayerId playerId) => (dispatch, getState) => {
            var state = getState();
            var pawn = state.GetPlayerPawn(playerId);

            if (pawn != PawnId.Empty) {
                var to = ChooseTarget(pawn, state.GetPlayerTargets(playerId));
                if (to != PawnId.Empty) {
                    if (state.GetCombatantLockOnTarget(pawn) != PawnId.Empty) {
                        dispatch(_combatantActions.LockOff(pawn));
                    } else {
                        dispatch(_combatantActions.LockOn(pawn, to));
                    }
                }
            }
        };

        private PawnId ChooseTarget(PawnId pawn, IEnumerable<PawnId> targets) {
            var pawnInstance = _pawnInstances.Get(pawn);
            var centroid = pawnInstance.Position;
            var forward = pawnInstance.Forward;

             return targets
                .Select(target => _pawnInstances.Get(target))
                .Select(instance => (
                    Id: instance.PawnId,
                    Distance: (instance.Position - centroid).sqrMagnitude,
                    Dot: Vector3.Dot(instance.Position - centroid, forward)
                ))
                .Where(target => target.Dot >= 0f)
                .OrderBy(target => target.Distance)
                .Select(target => target.Id)
                .FirstOrDefault();
        }
    }
}