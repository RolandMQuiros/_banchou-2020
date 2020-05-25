using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

using Banchou.Pawn;

namespace Banchou.Player.Targeting {
    public class TargetingGizmos : MonoBehaviour {
        private IEnumerable<IPawnInstance> _instances = Enumerable.Empty<IPawnInstance>();
        private IPawnInstance _target = null;
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances
        ) {
            observeState
                .Select(state => state.GetPlayerTargets(playerId))
                .Where(targets => targets.Any())
                .DistinctUntilChanged()
                .Select(targets => targets.Select(t => pawnInstances.Get(t)))
                .Subscribe(instances => {
                    _instances = instances;
                })
                .AddTo(this);

            observeState
                .Select(state => state.GetPlayerLockOnTarget(playerId))
                .Where(target => target != PawnId.Empty)
                .DistinctUntilChanged()
                .Select(target => pawnInstances.Get(target))
                .Subscribe(instance => { _target = instance; })
                .AddTo(this);
        }

        private void OnDrawGizmos() {
            var instanceCount = _instances.Count();
            var current = instanceCount;
            var ordered = _instances.PrioritizeTargets(Camera.main.transform, true);

            foreach (var instance in ordered) {
                var colorScale = (float)current-- / instanceCount;
                Gizmos.color = colorScale * Color.white;
                Gizmos.DrawWireCube(instance.Position, Vector3.one * 1.5f);
            }

            foreach (var instance in ordered) {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(instance.Position, Camera.main.transform.position);
            }

            if (_target != null) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_target.Position, 1f);
            }
        }
    }
}