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
            Transform getAnchor(PawnId pawnId) {
                var instance = pawnInstances.Get(pawnId) as PawnContext;
                return instance?.GetComponentInChildren<Targetable>()?.transform ?? instance?.transform;
            }

            var targetGroup = GetComponent<CinemachineTargetGroup>();

            observeState
                .Select(state => state.GetPlayerPawn(playerId))
                .StartWith(PawnId.Empty)
                .DistinctUntilChanged()
                .Pairwise()
                .Subscribe(pair => {
                    var prevAnchor = getAnchor(pair.Previous);
                    if (prevAnchor != null && targetGroup.FindMember(prevAnchor) != -1) {
                        targetGroup.RemoveMember(prevAnchor);
                    }

                    var nextAnchor = getAnchor(pair.Current);
                    if (nextAnchor != null) {
                        var index = targetGroup.FindMember(nextAnchor);
                        if (index != -1) {
                            targetGroup.m_Targets[index].weight = 1f;
                        } else {
                            targetGroup.AddMember(nextAnchor, 1f, 1f);
                        }
                    }
                })
                .AddTo(this);
        }
    }
}