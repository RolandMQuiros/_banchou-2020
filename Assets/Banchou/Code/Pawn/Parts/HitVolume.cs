using UnityEngine;
using Redux;

using Banchou.Player;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(Collider))]
    public class HitVolume : MonoBehaviour {
        private PawnId _pawnId;
        private Dispatcher _dispatch;
        private PlayerActions _actions;

        public void Construct(PawnId pawnId, Dispatcher dispatch, PlayerActions playerActions) {
            _pawnId = pawnId;
            _dispatch = dispatch;
            _actions = playerActions;
        }

        private void OnTriggerEnter(Collider collider) {
            var hurtVolume = collider.GetComponent<HurtVolume>();

        }
    }
}