using System;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Redux;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Player.Targeting {
    [RequireComponent(typeof(Collider))]
    public class TargetingVolume : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            GetState getState,
            Dispatcher dispatch,
            TargetingActions targetingActions,
            IPawnInstances pawnInstances,
            IObservable<GameState> observeState
        ) {

            var selectedPawnsDelta = observeState
                .Select(state => state.GetPlayerSelectedPawns(playerId))
                .Where(selected => selected != null && selected.Any())
                .DistinctUntilChanged();

            // Set position to centroid of selected pawns
            this.FixedUpdateAsObservable()
                .WithLatestFrom(selectedPawnsDelta, (_, selected) => selected)
                .Subscribe(selected => {
                    var centroid = selected
                        .Select(id => pawnInstances.Get(id))
                        .Aggregate(Vector3.zero, (sum, instance) => sum + instance.Position) / selected.Count();

                    transform.position = centroid;
                })
                .AddTo(this);

            this.OnTriggerEnterAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable != null && !getState().DoesPlayerHavePawn(playerId, targetable.PawnId)) {
                        dispatch(targetingActions.AddTarget(playerId, targetable.PawnId));
                    }
                })
                .AddTo(this);

            this.OnTriggerExitAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable != null && !getState().DoesPlayerHavePawn(playerId, targetable.PawnId)) {
                        dispatch(targetingActions.RemoveTarget(playerId, targetable.PawnId));
                    }
                })
                .AddTo(this);
        }
    }
}