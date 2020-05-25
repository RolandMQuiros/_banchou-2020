using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Banchou.Pawn;

namespace Banchou.Player.Targeting {
    public static class TargetingMethods {
        public static IOrderedEnumerable<IPawnInstance> PrioritizeTargets(this IEnumerable<IPawnInstance> pawns, Transform camera, bool filterHidden) {
            return pawns.Select(target => (
                    Instance: target,
                    Dot: Vector3.Dot((target.Position - camera.position).normalized, camera.forward)
                ))
                .Where(target => !filterHidden || target.Dot > 0.4f)
                .OrderByDescending(target => target.Dot)
                .Select(target => target.Instance)
                .OrderBy(_ => 1);
                // .OrderBy(target => Vector3.Distance(camera.position, target.transform.position));
        }
    }
}