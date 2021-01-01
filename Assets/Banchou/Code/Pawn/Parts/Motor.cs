using System;
using UnityEngine;

namespace Banchou.Pawn.Part {
    public interface IMotor {
        IObservable<(Vector3 Position, float When)> History { get; }
        void Teleport(Vector3 position);
        void Move(Vector3 velocity);
        Vector3 Project(Vector3 velocity);
    }
}