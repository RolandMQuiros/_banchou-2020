using System.Net;

using MessagePack;
using Banchou.Pawn;

namespace Banchou.Player {
    namespace StateAction {
        public interface IPlayerAction {
            PlayerId PlayerId { get; }
        }

        [MessagePackObject]
        public struct AddPlayer {
            [Key(0)] public PlayerId PlayerId;
            [Key(1)] public InputSource Source;
            [Key(2)] public IPEndPoint IP;
            [Key(3)] public int PeerId;
        }

        [MessagePackObject]
        public struct RemovePlayer {
            [Key(0)] public PlayerId PlayerId;
        }

        [MessagePackObject]
        public struct AttachPlayerToPawn : IPlayerAction {
            [Key(0)] public PlayerId PlayerId { get; set; }
            [Key(0)] public PawnId PawnId;
        }

        [MessagePackObject]
        public struct DetachPlayerFromPawn : IPlayerAction {
            [Key(0)] public PlayerId PlayerId { get; set; }
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