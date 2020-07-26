using System.Linq;
using UnityEngine;
using Redux;

using Banchou.Player;

namespace Banchou.Pawn.Part {
    public class AttachToAvailablePlayer : MonoBehaviour {
        private PawnId _pawnId;
        private GetState _getState;
        private Dispatcher _dispatch;
        private PlayerActions _playerActions;

        public void Construct(
            PawnId pawnId,
            GetState getState,
            Dispatcher dispatch,
            PlayerActions playerActions
        ) {
            _pawnId = pawnId;
            _getState = getState;
            _dispatch = dispatch;
            _playerActions = playerActions;
        }

        private void Start() {
            var playerId = _getState().Players
                .Where(p => p.Value.Source == InputSource.Local)
                .Select(p => p.Key)
                .First();
            _dispatch(_playerActions.Attach(playerId, _pawnId));
        }
    }
}