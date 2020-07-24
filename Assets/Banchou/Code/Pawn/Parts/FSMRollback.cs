using UnityEngine;

namespace Banchou.Pawn.Part {
    public class FSMRollback : MonoBehaviour {
        private Animator _animator;
        private void Awake() {
            _animator = GetComponent<Animator>();
        }

        private void Update() {
            if (Input.GetKey(KeyCode.Backslash)) {
                _animator.playableGraph.Evaluate(Time.fixedDeltaTime);
                _animator.playableGraph.Evaluate(Time.fixedDeltaTime);
                _animator.playableGraph.Evaluate(Time.fixedDeltaTime);
            }
        }
    }
}
