using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Player {
    public static class PlayerInputSelectors {
        public static Vector3 CameraPlaneProject(this Vector2 input, Vector3 cameraForward, Vector3 cameraRight, Vector3 plane) {
            return Vector3.ProjectOnPlane(cameraForward, plane).normalized * input.y +
                Vector3.ProjectOnPlane(cameraRight, plane).normalized * input.x;
        }

        public static Vector3 CameraPlaneProject(this Vector2 input, Vector3 cameraForward, Vector3 cameraRight) {
            return input.CameraPlaneProject(cameraForward, cameraRight, Vector3.up);
        }

        public static Vector3 CameraPlaneProject(this Vector2 input) {
            return input.CameraPlaneProject(Camera.main.transform.forward, Camera.main.transform.right, Vector3.up);
        }

        public static StickDirection ToStick(
            this Vector2 input,
            Vector3 orientation,
            Vector3 cameraForward,
            Vector3 cameraRight,
            Vector3 plane
        ) {
            var projected = input.CameraPlaneProject(cameraForward, plane);
            var angle = Vector3.Angle(orientation, projected);

            if (angle > -22.5f && angle <= 22.5f) {
                return StickDirection.Forward;
            } else if (angle > 22.5f && angle <= 67.5f) {
                return StickDirection.ForwardRight;
            } else if (angle > 67.5f && angle <= 112.5) {
                return StickDirection.Right;
            } else if (angle > 112.5 && angle <= 157.5) {
                return StickDirection.BackRight;
            } else if (angle > 157.5 && angle <= 202.5) {
                return StickDirection.Back;
            } else if (angle > 202.5 && angle <= 247.5) {
                return StickDirection.BackLeft;
            } else if (angle > 247.5 && angle <= 292.5) {
                return StickDirection.Left;
            } else if (angle > 292.5 && angle <= 337.5) {
                return StickDirection.ForwardLeft;
            } else if (angle > 337.5 && angle <= 382.5) {
                return StickDirection.Forward;
            }

            return StickDirection.Neutral;
        }

        public static StickDirection ToStick(this Vector2 input, Vector3 orientation, Vector3 cameraForward, Vector3 cameraRight) {
            return input.ToStick(orientation, cameraForward, cameraRight, Vector3.up);
        }

        public static StickDirection ToStick(this Vector2 input, Vector3 orientation, Vector3 plane) {
            return input.ToStick(orientation, Camera.main.transform.forward, Camera.main.transform.right, plane);
        }

        public static StickDirection ToStick(this Vector2 input, Vector3 orientation) {
            return input.ToStick(orientation, Camera.main.transform.forward, Camera.main.transform.right, Vector3.up);
        }
    }
}