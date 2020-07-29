using System;
using System.Collections;
using System.Linq;
using UnityEngine;

using Cinemachine;
using UniRx;


using Banchou.Pawn;
using Banchou.Pawn.Part;
using Banchou.Combatant;

namespace Banchou.Player.Targeting {
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class CombatantTargetGroup : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances
        ) {
            var targetGroup = GetComponent<CinemachineTargetGroup>();
            Func<PawnId, Transform> getAnchor = pawnId => {
                var instance = pawnInstances.Get(pawnId) as PawnContext;
                return instance?.GetComponentInChildren<Targetable>().transform ?? instance?.transform;
            };

            // Add pawns controlled by player, and targets locked onto by player
            observeState
                .Select(state => (
                    Pawn: state.GetPlayerPawn(playerId),
                    Targets: state.GetPlayerTargets(playerId),
                    LockOnTarget: state.GetCombatantLockOnTarget(state.GetPlayerPawn(playerId))
                ))
                .DistinctUntilChanged()
                .SelectMany(selection => selection.Targets
                    .Where(target => target != selection.LockOnTarget)
                    .Select(pawnId => ( Anchor: getAnchor(pawnId), Weight: 0.5f ))
                    .Append((
                        Anchor: getAnchor(selection.Pawn),
                        Weight: 1f
                    ))
                    .Append((
                        Anchor: getAnchor(selection.LockOnTarget),
                        Weight: 1f
                    ))
                )
                .CatchIgnoreLog()
                .Subscribe(target => {
                    var index = targetGroup.FindMember(target.Anchor);
                    if (index == -1) {
                        targetGroup.AddMember(target.Anchor, target.Weight, 1f);
                    } else {
                        targetGroup.m_Targets[index].weight = target.Weight;
                    }
                })
                .AddTo(this);

            // Remove pawns
            observeState
                .Select(state => (
                    Pawn: state.GetPlayerPawn(playerId),
                    Targets: state.GetPlayerTargets(playerId)
                ))
                .DistinctUntilChanged()
                .Select(selection => selection.Targets.Append(selection.Pawn))
                .Pairwise()
                .SelectMany(pair => pair.Previous.Except(pair.Current))
                .Select(target => getAnchor(target))
                .Where(anchor => anchor != null && targetGroup.FindMember(anchor) != -1)
                .CatchIgnoreLog()
                .Subscribe(anchor => { targetGroup.RemoveMember(anchor); })
                .AddTo(this);
        }
    }
}