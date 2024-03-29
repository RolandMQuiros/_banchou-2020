﻿using System.Collections.Generic;

namespace Banchou.Mob {
    public static class MobsReducers {
        public static MobsState Reduce(in MobsState prev, in object action) {
            if (action is StateAction.AddMob add) {
                MobState prevMob;
                if (!prev.States.TryGetValue(add.PawnId, out prevMob)) {
                    return new MobsState(prev) {
                        States = new Dictionary<Pawn.PawnId, MobState>(prev.States) {
                            [add.PawnId] = new MobState()
                        },
                        LastUpdated = add.When
                    };
                }
            }

            if (action is StateAction.RemoveMob remove) {
                var next = new MobsState(prev) {
                    States = new Dictionary<Pawn.PawnId, MobState>(prev.States),
                    LastUpdated = remove.When
                };
                next.States.Remove(remove.PawnId);
                return next;
            }

            if (action is StateAction.IMobAction mobAction) {
                MobState prevMob;
                if (prev.States.TryGetValue(mobAction.PawnId, out prevMob)) {
                    return new MobsState(prev) {
                        States = new Dictionary<Pawn.PawnId, MobState>(prev.States) {
                            [mobAction.PawnId] = ReduceMob(prevMob, mobAction)
                        },
                        LastUpdated = mobAction.When
                    };
                }
            }

            return prev;
        }

        private static MobState ReduceMob(in MobState prev, in object action) {
            if (action is StateAction.MobApproachPosition approachPosition) {
                return new MobState(prev) {
                    Stage = ApproachStage.Position,
                    ApproachPosition = approachPosition.Position
                };
            }

            if (action is StateAction.MobApproachTarget approachTarget) {
                return new MobState(prev) {
                    Stage = ApproachStage.Target,
                    Target = approachTarget.TargetId
                };
            }

            if (action is StateAction.MobApproachInterrupted approachInterrupted) {
                return new MobState(prev) {
                    Stage = ApproachStage.Interrupted
                };
            }

            if (action is StateAction.MobApproachCompleted approachCompleted) {
                return new MobState(prev) {
                    Stage = ApproachStage.Complete
                };
            }

            return prev;
        }
    }
}