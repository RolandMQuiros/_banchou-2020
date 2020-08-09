﻿using System.Net;
using Redux;

using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Player {
    namespace StateAction {
        public interface IPlayerAction {
            PlayerId PlayerId { get; }
        }

        public struct AddPlayer {
            public PlayerId PlayerId;
            public string PrefabKey;
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
        public ActionsCreator<GameState> AddLocalPlayer(PlayerId playerId, string prefabKey) => (dispatch, getState) => {
            dispatch(new StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = prefabKey,
                IP = getState().GetIP()
            });
        };

        public StateAction.AddPlayer AddPlayer(PlayerId playerId, string prefabKey, IPEndPoint ip, int peerId) {
            return new StateAction.AddPlayer {
                PlayerId = playerId,
                PrefabKey = prefabKey,
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