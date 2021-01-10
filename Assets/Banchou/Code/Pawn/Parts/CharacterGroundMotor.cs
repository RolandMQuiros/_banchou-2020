using System.Collections.Generic;
using UnityEngine;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(CharacterController))]
    public class CharacterGroundMotor : MonoBehaviour, IMotor {
        [SerializeField] private LayerMask _terrainMask;
        public Vector3 TargetPosition => _position + _velocity;

        private CharacterController _controller = null;
        private GetTime _getTime = null;
        private List<Vector3> _contacts = new List<Vector3>();
        private Vector3 _velocity = Vector3.zero;

        private Vector3 _position;

        public void Construct(
            CharacterController controller,
            GetTime getTime
        ) {
            _controller = controller;
            _getTime = getTime;
            _position = transform.position;
        }

        #region MonoBehaviour
        private void FixedUpdate() {
            Apply();
        }

        #endregion

        public void Teleport(Vector3 position) {
            var oldEnabled = _controller.enabled;
            _controller.enabled = false;
            _position = position;
            _controller.transform.position = _position;
            _controller.enabled = oldEnabled;
        }

        public void Move(Vector3 velocity) {
            _velocity += velocity;
        }

        public void Clear() {
            _velocity = Vector3.zero;
        }

        public void Apply() {
            Teleport(_position);
            if (_controller.Move(_velocity) != CollisionFlags.None) {
                _position = _controller.transform.position;
            } else {
                _position += _velocity;
            }

            _velocity = Vector3.zero;
            _contacts.Clear();
        }

        public Vector3 Project(Vector3 velocity) {
            return velocity;
        }
    }
}