using UnityEngine;
using System.Collections.Generic;

namespace Banchou.Pawn {
    public interface IPawnInstance {
        PawnId PawnId { get; }
        Vector3 Position { get; set; }
        Vector3 Forward { get; set; }
    }

    public interface IPawnInstances : IEnumerable<IPawnInstance> {
        IPawnInstance Get(PawnId pawnId);
        IEnumerable<IPawnInstance> GetMany(IEnumerable<PawnId> pawnIds);
        void Set(PawnId pawnId, IPawnInstance instance);
    }
}