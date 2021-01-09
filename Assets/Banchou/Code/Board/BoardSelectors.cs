namespace Banchou.Board {
    public static class BoardSelectors {
        public static BoardState GetBoard(this GameState state) {
            return state.Board;
        }

        public static float GetBoardTimescale(this GameState state) {
            return state.GetBoard().Timescale;
        }

        public static float GetBoardLastUpdated(this GameState state) {
            return state.GetBoard().LastUpdated;
        }
    }
}