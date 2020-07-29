using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UniRx;
using Redux;
using FluentBehaviourTree;

using Banchou.Pawn;
using Banchou.Combatant;
using Banchou.Mob;
using Banchou.Player;

namespace Banchou.AI {
    public class BasicTestEnemyBehaviour : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            MobActions mobActions,
            CombatantActions combatantActions,
            IPawnInstances pawnInstances,
            PlayerInputStreams playerInput
        ) {
            var trees = new List<IBehaviourTreeNode<GameState>>();

            var observeTrees = observeState
                .Select(state => state.GetPlayerPawn(playerId))
                .Where(pawnId => pawnId != PawnId.Empty)
                .DistinctUntilChanged()
                .Subscribe(pawnId => {
                    var pawn = pawnInstances.Get(pawnId) as PawnContext;
                    var body = pawn.Body;
                    var agent = pawn.Agent;
                    var targetId = PawnId.Empty;

                    var poked = false;

                    trees.Add(new BehaviourTreeBuilder<GameState>()
                        .Sequence("Poke and retreat")
                            .Do("Pick target", state => {
                                if (state.GetCombatantLockOnTarget(pawnId) != PawnId.Empty) {
                                    return BehaviourTreeStatus.Success;
                                } else {
                                    playerInput.PushCommand(playerId, InputCommand.LockOn, Time.fixedUnscaledTime);
                                    return BehaviourTreeStatus.Running;
                                }
                            })
                            .Do("Approach", state => {
                                var mob = state.GetMob(pawn.PawnId);
                                if (mob.Target == PawnId.Empty) {
                                    dispatch(mobActions.ApproachTarget(
                                        pawnId,
                                        state.GetCombatantLockOnTarget(pawn.PawnId),
                                        1f
                                    ));
                                } else if (state.IsMobApproachCompleted(pawn.PawnId)) {
                                    return BehaviourTreeStatus.Success;
                                } else if (state.IsMobApproachInterrupted(pawn.PawnId)) {
                                    return BehaviourTreeStatus.Failure;
                                }
                                return BehaviourTreeStatus.Running;
                            })
                            .Do("Poke", state => {
                                if (!poked) {
                                    playerInput.PushCommand(playerId, InputCommand.LightAttack);
                                    poked = true;
                                    return BehaviourTreeStatus.Running;
                                }
                                return BehaviourTreeStatus.Success;
                            })
                            .Do("Retreat", state => {
                                var target = state.GetCombatantLockOnTarget(pawnId);
                                var targetInstance = pawnInstances.Get(target);
                                if (target == PawnId.Empty || targetInstance == null) {
                                    return BehaviourTreeStatus.Failure;
                                }

                                var diff = pawn.transform.position - targetInstance.Position;
                                if (diff.magnitude < 6f) {
                                    if (!state.IsMobApproachingPosition(pawnId)) {
                                        dispatch(mobActions.ApproachPosition(pawnId, targetInstance.Position + (6f * diff.normalized)));
                                    }
                                } else if (state.IsMobApproachInterrupted(pawnId)) {
                                    return BehaviourTreeStatus.Failure;
                                } else if (state.IsMobApproachCompleted(pawnId)){
                                    return BehaviourTreeStatus.Success;
                                }
                                return BehaviourTreeStatus.Running;
                            })
                            .Do("Disengage", state => {
                                if (state.GetCombatantLockOnTarget(pawn.PawnId) != PawnId.Empty) {
                                    playerInput.PushCommand(playerId, InputCommand.LockOff, Time.fixedUnscaledTime);
                                    return BehaviourTreeStatus.Running;
                                }
                                poked = false;
                                return BehaviourTreeStatus.Success;
                            })
                        .End()
                        .Build()
                    );
                })
                .AddTo(this);

            observeState
                .DistinctUntilChanged()
                .ThrottleFrame(1, FrameCountType.EndOfFrame)
                .CatchIgnoreLog()
                .Subscribe(state => {
                    foreach (var tree in trees) {
                        tree.Tick(state);
                    }
                })
                .AddTo(this);
        }
    }
}
