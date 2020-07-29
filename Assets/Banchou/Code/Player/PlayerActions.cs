using System.Net;

using Banchou.Pawn;

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
}