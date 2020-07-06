namespace Banchou.Mob {
    public static class MobsReducers {
        public static MobsState Reduce(in MobsState prev, in object action) {
            var add = action as StateAction.AddMob;
            if (add != null) {
                MobState prevMob;
                if (!prev.TryGetValue(add.PawnId, out prevMob)) {
                    return new MobsState(prev) {
                        [add.PawnId] = new MobState()
                    };
                }
            }

            var remove = action as StateAction.RemoveMob;
            if (remove != null) {
                var next = new MobsState(prev);
                next.Remove(remove.PawnId);
                return next;
            }

            var mobAction = action as StateAction.MobAction;
            if (mobAction != null) {
                MobState prevMob;
                if (prev.TryGetValue(mobAction.PawnId, out prevMob)) {
                    return new MobsState(prev) {
                        [mobAction.PawnId] = ReduceMob(prevMob, mobAction)
                    };
                }
            }

            return prev;
        }

        private static MobState ReduceMob(in MobState prev, in object action) {
            var approachPosition = action as StateAction.MobApproachPosition;
            if (approachPosition != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Position,
                    ApproachPosition = approachPosition.Position
                };
            }

            var approachTarget = action as StateAction.MobApproachTarget;
            if (approachTarget != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Target,
                    Target = approachTarget.TargetId
                };
            }

            var approachInterrupted = action as StateAction.MobApproachInterrupted;
            if (approachInterrupted != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Interrupted
                };
            }

            var approachCompleted = action as StateAction.MobApproachCompleted;
            if (approachCompleted != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Complete
                };
            }

            return prev;
        }
    }
}