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
            PlayerActions playerActions,
            MobActions mobActions,
            CombatantActions combatantActions,
            IPawnInstances pawnInstances
        ) {
            var targets = new List<PawnContext>();
            var trees = new List<IBehaviourTreeNode<GameState>>();

            observeState
                .Select(state => state.GetPlayerTargets(playerId))
                .DistinctUntilChanged()
                .Pairwise()
                .SelectMany(pair => pair.Current.Except(pair.Previous))
                .Select(targetId => pawnInstances.Get(targetId) as PawnContext)
                .Subscribe(targetInstance => { targets.Add(targetInstance); })
                .AddTo(this);

            var observeTrees = observeState
                .Select(state => state.GetPlayerPawn(playerId))
                .DistinctUntilChanged()
                .Subscribe(pawnId => {
                    var pawn = pawnInstances.Get(pawnId) as PawnContext;
                    var body = pawn.Body;
                    var agent = pawn.Agent;
                    var targetId = PawnId.Empty;
                    Rigidbody target = null;

                    var poked = false;
                    var retreated = false;

                    trees.Add(new BehaviourTreeBuilder<GameState>()
                        .Sequence("Poke and retreat")
                            .Do("Pick target", state => {
                                if (state.GetCombatantTarget(pawnId) != PawnId.Empty) {
                                    return BehaviourTreeStatus.Success;
                                } else {
                                    dispatch(playerActions.LockOn(playerId));
                                    return BehaviourTreeStatus.Running;
                                }
                            })
                            .Do("Approach", state => {
                                var mob = state.GetMob(pawn.PawnId);
                                if (mob.Target == PawnId.Empty) {
                                    dispatch(mobActions.ApproachTarget(
                                        pawnId,
                                        state.GetCombatantTarget(pawn.PawnId),
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
                                    dispatch(combatantActions.PushCommand(pawnId, Command.LightAttack));
                                    poked = true;
                                    return BehaviourTreeStatus.Running;
                                }
                                return BehaviourTreeStatus.Success;
                            })
                            .Do("Retreat", state => {
                                if (retreated) {
                                    return BehaviourTreeStatus.Success;
                                }

                                var diff = pawn.transform.position - target.transform.position;
                                if (diff.magnitude > 6f) {
                                    dispatch(playerActions.Move(playerId, Vector3.zero));
                                    retreated = true;
                                    return BehaviourTreeStatus.Success;
                                } else {
                                    var direction = Vector3.Normalize(diff);
                                    dispatch(playerActions.Move(playerId, direction));
                                    return BehaviourTreeStatus.Running;
                                }
                            })
                            .Do("Disengage", state => {
                                targetId = state.GetCombatantTarget(pawn.PawnId);
                                if (targetId != PawnId.Empty) {
                                    dispatch(playerActions.LockOff(playerId));
                                    return BehaviourTreeStatus.Running;
                                }

                                target = null;
                                poked = false;
                                retreated = false;
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
                .CatchIgnore((Exception error) => { Debug.LogException(error); })
                .Subscribe(state => {
                    foreach (var tree in trees) {
                        tree.Tick(state);
                    }
                })
                .AddTo(this);
        }
    }
}
