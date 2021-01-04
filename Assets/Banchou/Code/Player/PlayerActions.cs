using System;

using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Player {
    namespace StateAction {
        public struct AddPlayer {
            public PlayerId PlayerId;
            public string PrefabKey;
            public Guid NetworkId;
            public string Name;
            public float When;
        }

        public struct RemovePlayer {
            public PlayerId PlayerId;
            public float When;
        }

        public interface IPlayerAction {
            PlayerId PlayerId { get; }
            float When { get; }
        }

        public struct AttachPlayerToPawn : IPlayerAction {
            public PlayerId PlayerId { get; set; }
            public PawnId PawnId;
            public float When { get; set; }
        }

        public struct DetachPlayerFromPawn : IPlayerAction {
            public PlayerId PlayerId { get; set; }
            public float When { get; set; }
        }
    }

    public class PlayersActions {
        private GetServerTime _getServerTime;

        public PlayersActions(GetServerTime getServerTime) {
            _getServerTime = getServerTime;
        }

        public StateAction.AddPlayer AddPlayer(
            PlayerId playerId,
            string prefabKey = null,
            string name = null,
            Guid networkId = default(Guid),
            float? when = null
        ) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = prefabKey,
                Name = name,
                NetworkId = networkId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.RemovePlayer Remove(PlayerId playerId, float? when = null) {
            return new StateAction.RemovePlayer {
                PlayerId = playerId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.AttachPlayerToPawn Attach(PlayerId playerId, PawnId pawnId, float? when = null) {
            return new StateAction.AttachPlayerToPawn {
                PlayerId = playerId,
                PawnId = pawnId,
                When = when ?? _getServerTime()
            };
        }

        public StateAction.DetachPlayerFromPawn Detach(PlayerId playerId, float? when = null) {
            return new StateAction.DetachPlayerFromPawn {
                PlayerId = playerId,
                When = when ?? _getServerTime()
            };
        }
    }
}