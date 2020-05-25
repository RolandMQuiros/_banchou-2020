using UnityEngine;

namespace Banchou.Pawn.Part {
    public interface IMotor {
        void Move(Vector3 velocity);
        Vector3 Project(Vector3 velocity);
    }
}