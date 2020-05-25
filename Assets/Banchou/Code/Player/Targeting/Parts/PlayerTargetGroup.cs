using System;
using System.Linq;
using UnityEngine;
using Cinemachine;
using UniRx;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Player.Targeting {
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class PlayerTargetGroup : MonoBehaviour {

        [Header("Debug")]
        [SerializeField] private string[] _targetingHistory = null;
        [SerializeField] private string _currentTarget = null;
        [SerializeField] private string[] _targetingFuture = null;

        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances
        ) {
            var targetGroup = GetComponent<CinemachineTargetGroup>();

            var targetPawnsDelta = observeState
                .Select(state => state.GetPlayer(playerId))
                .Where(player => player != null)
                .DistinctUntilChanged()
                .Select(player => {
                    if (player.LockOnTarget != PawnId.Empty) {
                        return player.SelectedPawns.Append(player.LockOnTarget);
                    }
                    return player.SelectedPawns;
                })
                .Pairwise();

            // Add pawns controlled by player, and targets locked onto by player
            targetPawnsDelta
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
                    targetGroup.AddMember(pair.Anchor, 1f, 1f);
                })
                .AddTo(this);

            // Remove pawns
            targetPawnsDelta
                .SelectMany(delta => delta.Previous.Except(delta.Current))
                .Select(target => pawnInstances.Get(target) as PawnContext)
                .Select(instance => instance?.GetComponentInChildren<Targetable>()?.transform ?? instance?.transform)
                .Where(anchor => anchor != null && targetGroup.FindMember(anchor) != -1)
                .Subscribe(anchor => { targetGroup.RemoveMember(anchor); })
                .AddTo(this);

            observeState
                .Select(state => state.GetPlayer(playerId))
                .Where(player => player != null)
                .Select(player => (
                    History: player.LockOnHistory,
                    Target: player.LockOnTarget,
                    Future: player.LockOnFuture
                ))
                .Subscribe(targeting => {
                    _targetingHistory = targeting.History.Select(t => t.ToString()).ToArray();
                    _currentTarget = targeting.Target.ToString();
                    _targetingFuture = targeting.Future.Select(t => t.ToString()).ToArray();
                })
                .AddTo(this);


            Observable.EveryFixedUpdate()
                .WithLatestFrom(observeState, (_, state) => state.GetPlayer(playerId))
                .Subscribe(player => {
                    // If locked on, rotates this group to face perpendicluar to the vector between the selected pawns and the locked-on target.
                    // This lets Cinemachine to re-center to a side-view of combat
                    if (player.LockOnTarget != PawnId.Empty) {
                        var target = pawnInstances.Get(player.LockOnTarget);
                        if (target != null) {
                            // Find the centroid of all selected Pawns
                            var selectedCentroid = player.SelectedPawns
                                .Select(id => pawnInstances.Get(id))
                                .Aggregate(Vector3.zero, (sum, instance) => sum + instance.Position)
                                / player.SelectedPawns.Count();
                            var diff = target.Position - selectedCentroid;

                            // Calculate a vector perpendicular to the vector between the centroid and the target Pawn
                            var forward = Vector3.Cross(diff, Vector3.up).normalized;
                            if (Vector3.Dot(forward, Camera.main.transform.forward) < 0f) {
                                forward *= -1;
                            }
                            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                        }
                    }
                    else {
                        var forward = player.SelectedPawns
                            .Select(id => pawnInstances.Get(id))
                            .Aggregate(Vector3.zero, (sum, instance) => sum + instance.Forward) /
                            player.SelectedPawns.Count();
                        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                    }
                })
                .AddTo(this);
        }
    }
}