namespace Banchou.Mob {
    public static class MobsReducers {
        public static MobsState Reduce(in MobsState prev, in object action) {
            var add = action as StateAction.Add;
            if (add != null) {
                MobState prevMob;
                if (!prev.TryGetValue(add.PawnId, out prevMob)) {
                    return new MobsState(prev) {
                        [add.PawnId] = new MobState()
                    };
                }
            }

            var remove = action as StateAction.Remove;
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
            var approachPosition = action as StateAction.ApproachPosition;
            if (approachPosition != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Position,
                    ApproachPosition = approachPosition.Position
                };
            }

            var approachTarget = action as StateAction.ApproachTarget;
            if (approachTarget != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Target,
                    Target = approachTarget.TargetId
                };
            }

            var approachInterrupted = action as StateAction.ApproachInterrupted;
            if (approachInterrupted != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Interrupted
                };
            }

            var approachCompleted = action as StateAction.ApproachCompleted;
            if (approachCompleted != null) {
                return new MobState(prev) {
                    Stage = ApproachStage.Complete
                };
            }

            return prev;
        }
    }
}