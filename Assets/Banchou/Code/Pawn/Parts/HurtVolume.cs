using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Pawn.Part {
    public class HurtVolume : MonoBehaviour {
        public PawnId PawnId { get; private set; }
        public void Construct(PawnId pawnId) {
            PawnId = pawnId;
        }
    }
}