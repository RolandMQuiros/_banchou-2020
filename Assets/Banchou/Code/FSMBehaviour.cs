using System;
using System.Collections.Generic;

using UnityEngine;
using UniRx;

namespace Banchou {
    public class FSMBehaviour : StateMachineBehaviour {
        public bool IsStateActive { get; private set; }
        private List<IDisposable> _streams = new List<IDisposable>();
        protected ICollection<IDisposable> Streams { get => _streams; }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        protected Subject<AnimatorStateInfo> ObserveStateEnter = new Subject<AnimatorStateInfo>();
        protected Subject<AnimatorStateInfo> ObserveStateUpdate = new Subject<AnimatorStateInfo>();
        protected Subject<AnimatorStateInfo> ObserveStateExit = new Subject<AnimatorStateInfo>();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            IsStateActive = true;
            ObserveStateEnter.OnNext(stateInfo);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            IsStateActive = false;
            ObserveStateExit.OnNext(stateInfo);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            ObserveStateUpdate.OnNext(stateInfo);
        }

        private void OnDisable() {
            IsStateActive = false;
        }

        private void OnDestroy() {
            _streams.ForEach(s => s.Dispose());
            _streams.Clear();
        }
    }
}