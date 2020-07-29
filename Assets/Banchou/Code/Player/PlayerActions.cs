using System.Net;
using System.Linq;
using System.Collections.Generic;

using Redux;
using Redux.DevTools;
using UnityEngine;

using Banchou.Pawn;
using Banchou.Combatant;

namespace Banchou.Player {
    namespace StateAction {
        public interface IPlayerAction {
            PlayerId PlayerId { get; }
        }

        public struct AddPlayer {
            public PlayerId PlayerId;
            public InputSource Source;
            public IPEndPoint IP;
            public int PeerId;
        }

        public struct RemovePlayer {
            public PlayerId PlayerId;
        }

        public struct AttachPlayerToPawn : IPlayerAction {
            public PlayerId PlayerId { get; set; }
            public PawnId PawnId;
        }

        public struct DetachPlayerFromPawn : IPlayerAction {
            public PlayerId PlayerId { get; set; }
        }

        public struct AddPlayerTarget : IPlayerAction {
            public PlayerId PlayerId { get; set; }
            public PawnId Target;
        }

        public struct RemovePlayerTarget : IPlayerAction {
            public PlayerId PlayerId { get; set; }
            public PawnId Target;
        }
    }

    public class PlayersActions {
        public StateAction.AddPlayer AddLocalPlayer(PlayerId playerId) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                Source = InputSource.Local
            };
        }

        public StateAction.AddPlayer AddAIPlayer(PlayerId playerId) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                Source = InputSource.AI
            };
        }

        public StateAction.AddPlayer AddNetworkPlayer(PlayerId playerId, IPEndPoint ip, int peerId) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                Source = InputSource.Network,
                IP = ip,
                PeerId = peerId
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
    }

    public class PlayerTargetingActions {
        private PlayerId _playerId;
        private IPawnInstances _pawnInstances;
        private CombatantActions _combatantActions;

        public PlayerTargetingActions(
            PlayerId playerId,
            IPawnInstances pawnInstances,
            CombatantActions combatantActions
        ) {
            _playerId = playerId;
            _pawnInstances = pawnInstances;
            _combatantActions = combatantActions;
        }

        public StateAction.AddPlayerTarget AddTarget(PawnId target) {
            return new StateAction.AddPlayerTarget {
                PlayerId = _playerId,
                Target = target
            };
        }

        public ActionsCreator<GameState> RemoveTarget(PawnId target) => (dispatch, getState) => {
            dispatch(new StateAction.RemovePlayerTarget {
                PlayerId = _playerId,
                Target = target
            });

            var pawn = getState().GetPlayerPawn(_playerId);
            if (getState().GetCombatantLockOnTarget(pawn) == target) {
                dispatch(_combatantActions.LockOff(pawn));
            }
        };

        public ActionsCreator<GameState> LockOn() => (dispatch, getState) => {
            var player = getState().GetPlayer(_playerId);
            var pawn = getState().GetPlayerPawn(_playerId);

            if (player != null && pawn != PawnId.Empty) {
                var to = ChooseTarget(pawn, getState().GetPlayerTargets(_playerId));

                dispatch(_combatantActions.LockOn(
                    combatantId: pawn,
                    to: to
                ));
            }
        };

        public ActionsCreator<GameState> LockOff() => (dispatch, getState) => {
            var pawn = getState().GetPlayerPawn(_playerId);
            if (pawn != PawnId.Empty) {
                dispatch(_combatantActions.LockOff(pawn));
            }
        };

        public ActionsCreator<GameState> ToggleLockOn() => (dispatch, getState) => {
            var state = getState();
            var pawn = state.GetPlayerPawn(_playerId);

            if (pawn != PawnId.Empty) {
                var to = ChooseTarget(pawn, state.GetPlayerTargets(_playerId));
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