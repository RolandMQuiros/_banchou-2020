﻿using System.Collections.Generic;
using Banchou.Pawn;

namespace Banchou.Mob {
    public static class MobSelectors {
        public static IDictionary<PawnId, MobState> GetMobs(this GameState state) {
            return state.Board.Mobs.States;
        }

        public static MobState GetMob(this GameState state, PawnId pawnId) {
            MobState mob;
            if (state.GetMobs().TryGetValue(pawnId, out mob)) {
                return mob;
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