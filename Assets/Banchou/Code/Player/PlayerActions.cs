using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Redux;

using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;

namespace Banchou.Player {
    namespace StateAction {
        public class PlayerAction {
            public PlayerId PlayerId;
        }

        public class Add {
            public PlayerId PlayerId;
            public InputSource Source;
        }

        public class Remove {
            public PlayerId PlayerId;
        }

        public class Attach : PlayerAction {
            public PawnId PawnId;
        }

        public class Detach : PlayerAction { }

        public class Move : PlayerAction {
            public Vector2 Direction;
        }

        public class Look : PlayerAction {
            public Vector2 Direction;
        }

        public class AddTarget : PlayerAction {
            public PawnId Target;
        }

        public class RemoveTarget : PlayerAction {
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

        public StateAction.Add Add(PlayerId playerId, InputSource source) {
            return new StateAction.Add {
                PlayerId = playerId,
                Source = source
            };
        }

        public StateAction.Remove Remove(PlayerId playerId) {
            return new StateAction.Remove {
                PlayerId = playerId
            };
        }

        public StateAction.Attach Attach(PlayerId playerId, PawnId pawnId) {
            return new StateAction.Attach {
                PlayerId = playerId,
                PawnId = pawnId
            };
        }

        public StateAction.Detach Detach(PlayerId playerId) {
            return new StateAction.Detach {
                PlayerId = playerId
            };
        }

        public StateAction.Move Move(PlayerId playerId, Vector3 direction) {
            return new StateAction.Move {
                PlayerId = playerId,
                Direction = direction
            };
        }

        public StateAction.Look Look(PlayerId playerId, Vector2 direction) {
            return new StateAction.Look {
                PlayerId = playerId,
                Direction = direction
            };
        }

        public StateAction.AddTarget AddTarget(PlayerId playerId, PawnId target) {
            return new StateAction.AddTarget {
                PlayerId = playerId,
                Target = target
            };
        }

        public ActionsCreator<GameState> RemoveTarget(PlayerId playerId, PawnId target) => (dispatch, getState) => {
            dispatch(new StateAction.RemoveTarget {
                PlayerId = playerId,
                Target = target
            });

            var pawn = getState().GetPlayerPawn(playerId);
            if (getState().GetCombatantTarget(pawn) == target) {
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
                    if (state.GetCombatantTarget(pawn) != PawnId.Empty) {
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