using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Banchou {
    public static class UtilityExtensions {
        public static IEnumerable<Transform> BreadthFirstTraversal(this Transform root) {
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0) {
                var xform = queue.Dequeue();
                yield return xform;

                foreach (var child in xform) {
                    queue.Enqueue((Transform)child);
                }
            }
        }

        public static Vector3 Centroid(this IEnumerable<Vector3> positions) {
            return positions
                .Aggregate(Vector3.zero, (sum, position) => sum + position) / positions.Count();
        }
    }
}