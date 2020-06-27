using UnityEngine;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(Collider))]
    public class HitVolume : MonoBehaviour {
        public PawnId PawnId { get; private set; }
        public void Construct(PawnId pawnId) {
            PawnId = pawnId;
        }
    }
}