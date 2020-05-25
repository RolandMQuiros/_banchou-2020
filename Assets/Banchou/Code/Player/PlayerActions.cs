using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

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
            public IEnumerable<PawnId> Pawns;
        }

        public class Detach : PlayerAction {
            public IEnumerable<PawnId> Pawns;
        }

        public class DetachAll : PlayerAction { }

        public class Move : PlayerAction {
            public Vector2 Direction;
        }

        public class Look : PlayerAction {
            public Vector2 Direction;
        }

        public class PushCommand : PlayerAction {
            public Command Command;
            public float When;
        }
    }

    public class PlayerActions {
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

        public StateAction.Attach Attach(PlayerId playerId, params PawnId[] pawns) {
            return new StateAction.Attach {
                PlayerId = playerId,
                Pawns = pawns
            };
        }

        public StateAction.Attach Attach(PlayerId playerId, IEnumerable<PawnId> pawns) {
            return new StateAction.Attach {
                PlayerId = playerId,
                Pawns = pawns
            };
        }

        public StateAction.Detach Detach(PlayerId playerId, IEnumerable<PawnId> pawns) {
            return new StateAction.Detach {
                PlayerId = playerId,
                Pawns = pawns
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

        public StateAction.PushCommand PushCommand(PlayerId playerId, Command command, float when) {
            return new StateAction.PushCommand {
                PlayerId = playerId,
                Command = command,
                When = when
            };
        }
    }
}