using UnityEngine;
using Banchou.Pawn;

namespace Banchou.Mob {
    public static class MobSelectors {
        public static MobState GetMob(this GameState state, PawnId pawnId) {
            MobState mob;
            if (state.Mobs.TryGetValue(pawnId, out mob)) {
                return mob;
            }
            return null;
        }

        public static PawnId GetMobApproachTarget(this GameState state, PawnId pawnId) {
            var mob = state.GetMob(pawnId);
            if (mob?.Stage == ApproachStage.Target) {
                return mob.Target;
            }
            return PawnId.Empty;
        }

        public static Vector3? GetMobTargetPosition(this GameState state, PawnId pawnId) {
            var mob = state.GetMob(pawnId);
            if (mob?.Stage == ApproachStage.Position) {
                return mob?.Position;
            }
            return null;
        }

        public static bool IsMobApproachingTarget(this GameState state, PawnId pawnId) {
            return state.GetMob(pawnId)?.Stage == ApproachStage.Target;
        }

        public static bool IsMobApproachingPosition(this GameState state, PawnId pawnId) {
            return state.GetMob(pawnId)?.Stage == ApproachStage.Position;
        }

        public static bool IsMobApproaching(this GameState state, PawnId pawnId) {
            var mob = state.GetMob(pawnId);
            return mob?.Stage == ApproachStage.Target || mob?.Stage == ApproachStage.Position;
        }

        public static bool IsMobApproachInterrupted(this GameState state, PawnId pawnId) {
            return state.GetMob(pawnId)?.Stage == ApproachStage.Interrupted;
        }

        public static bool IsMobApproachCompleted(this GameState state, PawnId pawnId) {
            return state.GetMob(pawnId)?.Stage == ApproachStage.Complete;
        }
    }
}