using System;
using System.Linq;
using UnityEngine;

using Cinemachine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class CombatantTargetGroup : MonoBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            IPawnInstances pawnInstances
        ) {
            var targetGroup = GetComponent<CinemachineTargetGroup>();
            Func<PawnId, Transform> getAnchor = id => {
                var instance = pawnInstances.Get(id) as PawnContext;
                return instance?.GetComponentInChildren<Targetable>().transform ?? instance?.transform;
            };

            // Add pawns controlled by player, and targets locked onto by player
            observeState
                .Select(state => state.GetCombatant(pawnId))
                .StartWith(default(CombatantState))
                .DistinctUntilChanged()
                .Where(combatant => combatant != null)
                .SelectMany(combatant => combatant.Targets
                    .Where(target => target != combatant.LockOnTarget)
                    .Select(targetId => ( Anchor: getAnchor(targetId), Weight: 0.5f ))
                    .Append((
                        Anchor: getAnchor(pawnId),
                        Weight: 1f
                    ))
                    .Append((
                        Anchor: getAnchor(combatant.LockOnTarget),
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
                .Select(state => state.GetCombatant(pawnId))
                .DistinctUntilChanged()
                .Where(combatant => combatant != null)
                .Select(combatant => combatant.Targets.Append(pawnId))
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