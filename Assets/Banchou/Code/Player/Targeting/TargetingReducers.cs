using System.Linq;
using System.Collections.Generic;

using Banchou.Pawn;

namespace Banchou.Player.Targeting {
    public static class TargetingReducers {
        public static PlayerState Reduce(in PlayerState prev, in object action) {
            var addTarget = action as StateAction.AddTarget;
            if (addTarget != null && !prev.Targets.Contains(addTarget.Target)) {
                var targets = new HashSet<PawnId>(prev.Targets);
                targets.Add(addTarget.Target);

                var future = prev.LockOnFuture;
                if (prev.LockOnTarget != PawnId.Empty) {
                    future = future.Append(addTarget.Target);
                }

                return new PlayerState(prev) {
                    Targets = targets,
                    LockOnFuture = future
                };
            }

            var removeTarget = action as StateAction.RemoveTarget;
            if (removeTarget != null) {
                var target = removeTarget.Target;

                // Remove target from current targeting list
                var targets = prev.Targets;
                if (targets.Contains(target)) {
                    targets = new HashSet<PawnId>(prev.Targets);
                    targets.Remove(target);
                }

                // Remove the target from the history, if it's present
                var history = prev.LockOnHistory;
                if (history.Contains(target)) {
                    history = history.Where(id => id != target);
                }

                // Same with future
                var future = prev.LockOnFuture;
                if (future.Contains(target)) {
                    future = history.Where(id => id != target);
                }

                // If the current target was removed, end the lock-on
                var nextTarget = prev.LockOnTarget;
                if (nextTarget == target) {
                    nextTarget = PawnId.Empty;
                }

                // If any change is detected, update the PlayerState
                if (targets != prev.Targets || history != prev.LockOnHistory || future != prev.LockOnFuture || nextTarget != prev.LockOnTarget) {
                    return new PlayerState(prev) {
                        Targets = targets,
                        LockOnHistory = history,
                        LockOnFuture = future,
                        LockOnTarget = nextTarget
                    };
                }
            }

            var lockOn = action as StateAction.LockOn;
            if (lockOn != null && prev.Targets.Contains(lockOn.To)) {
                return new PlayerState(prev) {
                    LockOnTarget = lockOn.To,
                    LockOnFuture = prev.Targets.Where(id => id != lockOn.To),
                    LockOnHistory = Enumerable.Empty<PawnId>()
                };
            }

            var nextLockOn = action as StateAction.NextLockOn;
            if (nextLockOn != null && prev.Targets.Contains(nextLockOn.To) && prev.LockOnFuture.Contains(nextLockOn.To)) {
                // Add the previous target to the history
                var history = prev.LockOnHistory;
                if (prev.LockOnTarget != PawnId.Empty) {
                    history = history.Append(prev.LockOnTarget);
                }

                // Remove the current target from the future
                var future = prev.LockOnFuture.Where(id => id != nextLockOn.To);

                // If the future is empty, that means we've completed a cycle and can swap the collections
                if (!future.Any()) {
                    return new PlayerState(prev) {
                        LockOnHistory = future,
                        LockOnFuture = history,
                        LockOnTarget = nextLockOn.To
                    };
                }

                return new PlayerState(prev) {
                    LockOnHistory = history,
                    LockOnFuture = future,
                    LockOnTarget = nextLockOn.To
                };
            }

            var prevLockOn = action as StateAction.PreviousLockOn;
            if (prevLockOn != null && prev.Targets.Contains(prevLockOn.To)) {
                // Add the previous target back into the future
                var future = prev.LockOnFuture;
                if (prev.LockOnTarget != PawnId.Empty) {
                    future = future.Append(prev.LockOnTarget);
                }

                // If the new target is in the history, remove it
                var history = prev.LockOnHistory.Where(id => id != prevLockOn.To).ToList();

                // If the future is empty, that means we've completed a cycle and can swap the collections
                if (!history.Any()) {
                    return new PlayerState(prev) {
                        LockOnHistory = future,
                        LockOnFuture = history,
                        LockOnTarget = prevLockOn.To
                    };
                }

                return new PlayerState(prev) {
                    LockOnHistory = history,
                    LockOnFuture = future,
                    LockOnTarget = prevLockOn.To
                };
            }

            var lockOff = action as StateAction.LockOff;
            if (lockOff != null) {
                return new PlayerState(prev) {
                    LockOnHistory = Enumerable.Empty<PawnId>(),
                    LockOnFuture = Enumerable.Empty<PawnId>(),
                    LockOnTarget = PawnId.Empty
                };
            }

            return null;
        }
    }
}