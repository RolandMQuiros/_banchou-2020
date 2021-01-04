using UnityEngine;

namespace Banchou.Pawn.Part {
    public interface IMotor {
        Vector3 TargetPosition { get; }
        void Teleport(Vector3 position);
        void Move(Vector3 velocity);
        void Clear();
        void Apply();
        Vector3 Project(Vector3 velocity);
    }
}