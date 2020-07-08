namespace Redux.DevTools {
    public interface ICollapsibleAction {
        object Collapse(in object next);
    }
}