using System;
using Redux;
using Banchou.Pawn;

namespace Banchou.Player {
    namespace StateAction {
        public interface IPlayerAction {
            PlayerId PlayerId { get; }
        }

        public struct AddPlayer {
            public PlayerId PlayerId;
            public string PrefabKey;
            public Guid NetworkId;
            public string Name;
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
    }

    public class PlayersActions {
        public StateAction.AddPlayer AddPlayer(PlayerId playerId, string prefabKey = null, string name = null, Guid networkId = default(Guid)) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = prefabKey,
                Name = name,
                NetworkId = networkId
            };
        }

        public StateAction.AddPlayer AddPlayer(Guid networkId) {
            return new StateAction.AddPlayer {
                PlayerId = PlayerId.Create(),
                NetworkId = networkId
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
}