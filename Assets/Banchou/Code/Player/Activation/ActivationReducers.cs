using System.Linq;

namespace Banchou.Player.Activation {
    public static class ActivationReducers {
        public static PlayerState Reduce(in PlayerState prev, in object action) {
            var leftOn = action as StateAction.ActivateLeftPawn;
            if (leftOn != null && prev.Pawns.Count() >= 2) {
                return new PlayerState(prev) {
                    SelectedPawns = prev.Pawns.Skip(1).Take(1).ToList()
                };
            }

            var leftOff = action as StateAction.DeactivateLeftPawn;
            if (leftOff != null && prev.Pawns.Count() >= 2) {
                var selected = prev.SelectedPawns.ToList();
                selected.Remove(prev.Pawns[1]);

                return new PlayerState(prev) {
                    SelectedPawns = selected
                };
            }


            var rightOn = action as StateAction.ActivateRightPawn;
            if (rightOn != null && prev.Pawns.Count() >= 3) {
                return new PlayerState(prev) {
                    SelectedPawns = prev.Pawns.Skip(2).Take(1).ToList()
                };
            }

            var rightOff = action as StateAction.DeactivateRightPawn;
            if (rightOff != null && prev.Pawns.Count() >= 3) {
                var selected = prev.SelectedPawns.ToList();
                selected.Remove(prev.Pawns[2]);

                return new PlayerState(prev) {
                    SelectedPawns = selected
                };
            }

            var all = action as StateAction.ActivateAllPawns;
            if (all != null) {
                return new PlayerState(prev) {
                    SelectedPawns = prev.Pawns
                };
            }

            var reset = action as StateAction.ResetActivations;
            if (reset != null && prev.Pawns.Count() >= 1) {
                return new PlayerState(prev) {
                    SelectedPawns = prev.Pawns.Take(1).ToList()
                };
            }

            return null;
        }
    }
}