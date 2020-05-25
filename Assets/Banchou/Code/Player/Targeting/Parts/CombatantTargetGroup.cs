using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Cinemachine;
using UniRx;
using UniRx.Triggers;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Player.Targeting {
    public class CombatantTargetGroup : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            GetState getState,
            IPawnInstances pawnInstances
        ) {
            var targetGroup = GetComponent<CinemachineTargetGroup>();
            var selectedDelta = observeState
                .Select(state => (
                    Pawns: state.GetPlayerPawns(playerId),
                    Targets: state.GetPlayerTargets(playerId)
                ))
                .DistinctUntilChanged()
                .Select(state => state.Pawns.Concat(state.Targets))
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
                    var player = getState().GetPawnPlayer(pair.Instance.PawnId);

                    float weight = 0.5f;
                    if (player != null) {
                        switch (player.Source) {
                            case InputSource.LocalSingle:
                            case InputSource.LocalMulti:
                                weight = 1f;
                                break;
                        }
                    }

                    targetGroup.AddMember(pair.Anchor, weight, 1f);
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