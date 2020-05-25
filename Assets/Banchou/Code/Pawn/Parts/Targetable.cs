using UnityEngine;

namespace Banchou.Pawn.Part {
    public class Targetable : MonoBehaviour {
        public PawnId PawnId { get; private set; }
        public void Construct(PawnId pawnId) { PawnId = pawnId; }
    }
}