namespace Banchou.Player {
    public enum InputCommand : byte {
        None,
        LightAttack,
        HeavyAttack
    }

    public enum StickDirection : byte {
        Neutral,
        Forward,
        ForwardRight,
        Right,
        BackRight,
        Back,
        BackLeft,
        Left,
        ForwardLeft
    }
}