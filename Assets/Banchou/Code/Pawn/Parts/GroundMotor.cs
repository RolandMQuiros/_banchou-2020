using System.Collections.Generic;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Pawn.Part {
    [RequireComponent(typeof(Rigidbody))]
    public class GroundMotor : MonoBehaviour, IMotor {
        [SerializeField] private LayerMask _terrainMask;
        public Vector3 TargetPosition => _rigidbody.position + _velocity;

        private Rigidbody _rigidbody = null;
        private GetServerTime _getServerTime = null;
        private List<Vector3> _contacts = new List<Vector3>();
        private Vector3 _velocity = Vector3.zero;

        private class ContactSorter : IComparer<Vector3> {
            private Rigidbody _body;
            public ContactSorter(Rigidbody body) {
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
            Rigidbody rigidbody,
            GetServerTime getServerTime
        ) {
            _rigidbody = rigidbody;
            _getServerTime = getServerTime;
            _sorter = new ContactSorter(_rigidbody);
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
            _rigidbody.position = position;
        }

        public void Move(Vector3 velocity) {
            _velocity += Project(velocity);
        }

        public void Clear() {
            _velocity = Vector3.zero;
        }

        public void Apply() {
             var newPosition = _rigidbody.position + _velocity;
            _rigidbody.MovePosition(newPosition);
            _velocity = Vector3.zero;
            _contacts.Clear();
        }

        public Vector3 Project(Vector3 velocity) {
            var projected = velocity;

            _contacts.Sort(_sorter);
            foreach (var contact in _contacts) {
                // If we're moving into a surface, we want to project the movement direction on it, so we don't cause physics jitters from
                // overlaps
                if (Vector3.Dot(contact, _rigidbody.transform.up) > 0.3f) {
                    if (Vector3.Dot(velocity, contact) < 0f) {
                        // If surface is a floor, and we're moving into it, move along it at full movement speed
                        projected = Vector3.ProjectOnPlane(projected, contact).normalized * projected.magnitude;
                    }
                    // If we're moving away from the surface, no need for projections
                } else if (Vector3.Dot(velocity, contact) < 0f) {
                    // If the surface is a wall, and we're moving into it, move along it instead
                    projected = Vector3.ProjectOnPlane(projected, contact);
                }
            }

            return projected;
        }

        private void OnDrawGizmos() {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + _velocity * 3f);

            Gizmos.color = Color.blue;
            foreach (var contact in _contacts) {
                Gizmos.DrawLine(transform.position, transform.position + contact * 2.5f);
            }
        }
    }
}