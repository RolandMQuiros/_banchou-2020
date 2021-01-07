using System.Collections.Generic;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(CharacterController))]
    public class CharacterGroundMotor : MonoBehaviour, IMotor {
        [SerializeField] private LayerMask _terrainMask;
        public Vector3 TargetPosition => _controller.transform.position + _velocity;

        private CharacterController _controller = null;
        private GetServerTime _getServerTime = null;
        private List<Vector3> _contacts = new List<Vector3>();
        private Vector3 _velocity = Vector3.zero;

        private class ContactSorter : IComparer<Vector3> {
            private Transform _body;
            public ContactSorter(Transform body) {
                _body = body;
            }
            public int Compare(Vector3 first, Vector3 second) {
                var diff = Vector3.Dot(first, _body.transform.up) - Vector3.Dot(first, _body.transform.up);
                return (int)Mathf.Sign(diff);
            }
        }
        private ContactSorter _sorter;

        #region MonoBehaviour

        public void Construct(
            CharacterController controller,
            GetServerTime getServerTime
        ) {
            _controller = controller;
            _getServerTime = getServerTime;
            _sorter = new ContactSorter(_controller.transform);
        }

        private void OnCollisionStay(Collision collision) {
            foreach (var contact in collision.contacts) {
                if ((_terrainMask.value & (1 << contact.otherCollider.gameObject.layer)) != 0) {
                    _contacts.Add(contact.normal);
                }
            }
        }

        private void OnCollisionEnter(Collision collision) {
            OnCollisionStay(collision);
        }

        private void FixedUpdate() {
            Apply();
        }

        #endregion

        public void Teleport(Vector3 position) {
            var oldEnabled = _controller.enabled;
            _controller.enabled = false;
            _controller.transform.position = position;
            _controller.enabled = oldEnabled;
        }

        public void Move(Vector3 velocity) {
            _velocity += velocity;
        }

        public void Clear() {
            _velocity = Vector3.zero;
        }

        public void Apply() {
             _controller.Move(_velocity);
            _velocity = Vector3.zero;
            _contacts.Clear();
        }

        public Vector3 Project(Vector3 velocity) {
            return velocity;
        }
    }
}