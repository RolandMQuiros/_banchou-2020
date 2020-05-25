using UnityEngine;
using System.Collections.Generic;

namespace Banchou.Pawn {
    public interface IPawnInstance {
        PawnId PawnId { get; }
        Vector3 Position { get; }
        Vector3 Forward { get; }
    }

    public interface IPawnInstances {
        IPawnInstance Get(PawnId pawnId);
        IEnumerable<IPawnInstance> GetMany(IEnumerable<PawnId> pawnIds);
        void Set(PawnId pawnId, IPawnInstance instance);
    }
}