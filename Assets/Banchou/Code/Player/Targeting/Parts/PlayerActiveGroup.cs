using System;
using System.Linq;
using UnityEngine;
using UniRx;
using Cinemachine;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Player.Targeting {
    public class PlayerActiveGroup : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances
        ) {
            var targetGroup = GetComponent<CinemachineTargetGroup>();
            var selectedDelta = observeState
                .Select(state => state.GetPlayerPawns(playerId))
                .DistinctUntilChanged()
                .Pairwise();

            // Add pawns controlled by player, and targets locked onto by player
            selectedDelta
                .SelectMany(delta => delta.Current.Except(delta.Previous))
                .Select(target => pawnInstances.Get(target) as PawnContext)
                .Select(instance => (
                    Instance: instance,
                    Anchor: instance?.GetComponentInChildren<Targetable>()?.transform ?? instance?.transform
                ))
                .Where(pair => pair.Instance != null && targetGroup.FindMember(pair.Anchor) == -1)
                .Subscribe(pair => {
                    var bounds = pair.Instance.GetComponentsInChildren<Collider>()
                        .Where(collider => !collider.isTrigger)
                        .Select(collider => collider.bounds)
                        .Aggregate(new Bounds(), (total, next) => { total.Encapsulate(next); return total; });
                    var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

                    targetGroup.AddMember(pair.Anchor, 1f, radius);
                })
                .AddTo(this);

            // Remove pawns
            selectedDelta
                .SelectMany(delta => delta.Previous.Except(delta.Current))
                .Select(target => pawnInstances.Get(target) as PawnContext)
                .Select(instance => instance?.GetComponentInChildren<Targetable>()?.transform ?? instance?.transform)
                .Where(anchor => anchor != null && targetGroup.FindMember(anchor) != -1)
                .Subscribe(anchor => { targetGroup.RemoveMember(anchor); })
                .AddTo(this);
        }
    }
}