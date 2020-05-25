using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UniRx;
using Redux;
using FluentBehaviourTree;

using Banchou.Pawn;
using Banchou.Mob;
using Banchou.Player;
using Banchou.Player.Targeting;

namespace Banchou.AI {
    public class BasicTestEnemyBehaviour : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            MobActions mobActions,
            TargetingActions targetingActions,
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
                .Select(state => state.GetPlayerPawns(playerId))
                .DistinctUntilChanged()
                .Pairwise()
                .Select(pair => pair.Current.Except(pair.Previous))
                .Where(pawns => pawns.Any())
                .SelectMany(pawns => pawns.Select(p => pawnInstances.Get(p) as PawnContext))
                .Subscribe(pawn => {
                    var body = pawn.Body;
                    var agent = pawn.Agent;
                    var targetId = PawnId.Empty;
                    Rigidbody target = null;

                    var targetPicked = false;
                    var approached = false;
                    var poked = false;
                    var retreated = false;

                    trees.Add(new BehaviourTreeBuilder<GameState>()
                        .Sequence("Poke and retreat")
                            .Do("Pick target", state => {
                                if (!targetPicked) {
                                    targetId = state.GetPlayerLockOnTarget(playerId);
                                    if (targetId == PawnId.Empty) {
                                        dispatch(targetingActions.LockOn(playerId));
                                        return BehaviourTreeStatus.Running;
                                    } else {
                                        target = (pawnInstances.Get(targetId) as PawnContext)?.Body;
                                        targetPicked = true;
                                        dispatch(mobActions.ApproachTarget(pawn.PawnId, targetId, 2f));
                                        if (target == null) {
                                            return BehaviourTreeStatus.Failure;
                                        }
                                    }
                                }
                                return BehaviourTreeStatus.Success;
                            })
                            .Do("Approach", state => {
                                if (!approached) {
                                    var mob = state.GetMob(pawn.PawnId);

                                    switch (mob.Stage) {
                                        case ApproachStage.Target:
                                            return BehaviourTreeStatus.Running;
                                        case ApproachStage.Interrupted:
                                            return BehaviourTreeStatus.Failure;
                                    }
                                }

                                approached = true;
                                return BehaviourTreeStatus.Success;
                            })
                            .Do("Poke", state => {
                                if (!poked) {
                                    dispatch(playerActions.PushCommand(playerId, Command.LightAttack, Time.unscaledTime));
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
                                targetId = state.GetPlayerLockOnTarget(playerId);
                                if (targetId != PawnId.Empty) {
                                    dispatch(targetingActions.LockOff(playerId));
                                    return BehaviourTreeStatus.Running;
                                }

                                target = null;
                                targetPicked = false;
                                approached = false;
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

            // this.LateUpdateAsObservable()
            //     .Sample(TimeSpan.FromSeconds(1f / 15f))
            //     .WithLatestFrom(observeState, (_, state) => state)
            //     .Subscribe(state => {
            //         foreach (var tree in trees) {
            //             tree.Tick(state);
            //         }
            //     })
            //     .AddTo(this);
        }
    }
}
