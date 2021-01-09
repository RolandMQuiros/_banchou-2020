using System;
using UniRx;
using UnityEngine;
using Banchou.Network;
namespace Banchou.Pawn.Part {
    public class Orientation : MonoBehaviour {
        public IObservable<(Quaternion Rotation, float When)> History => _history;
        private ReactiveProperty<(Quaternion Rotation, float When)> _history = new ReactiveProperty<(Quaternion Rotation, float When)>();
        private GetTime _getTime;

        public void Construct(GetTime getTime) {
            _getTime = getTime;
        }

        public void TrackRotation(Quaternion rotation) {
            TrackRotation(rotation, _getTime());
        }

        public void TrackRotation(Quaternion rotation, float when) {
            _history.Value = (
                Rotation: rotation,
                When: when
            );
            transform.rotation = rotation;
        }

        public void TrackForward(Vector3 forward) {
            TrackForward(forward, _getTime());
        }
        public void TrackForward(Vector3 forward, float when) {
            TrackRotation(Quaternion.LookRotation(forward), when);
        }
    }
}