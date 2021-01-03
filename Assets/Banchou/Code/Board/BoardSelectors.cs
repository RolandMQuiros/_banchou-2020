namespace Banchou.Board {
    public static class BoardSelectors {
        public static float GetBoardRewindTime(this GameState state) {
            return state.Board.RewindTime;
        }
    }
}