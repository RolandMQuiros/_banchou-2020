using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Animations;
using UniRx;

namespace Banchou {
    public class FSMBehaviour : StateMachineBehaviour {
        public bool IsStateActive { get; private set; }

        private List<IDisposable> _streams = new List<IDisposable>();
        protected ICollection<IDisposable> Streams { get => _streams; }

        protected struct FSMUnit {
            public AnimatorStateInfo StateInfo;
            public int LayerIndex;
            public AnimatorControllerPlayable Playable;

            public void Deconstruct(
                out AnimatorStateInfo stateInfo,
                out int layerIndex,
                out AnimatorControllerPlayable playable
            ) {
                stateInfo = StateInfo;
                layerIndex = LayerIndex;
                playable = Playable;
            }
        }

        protected Subject<FSMUnit> ObserveStateEnter = new Subject<FSMUnit>();
        protected Subject<FSMUnit> ObserveStateUpdate = new Subject<FSMUnit>();
        protected Subject<FSMUnit> ObserveStateExit = new Subject<FSMUnit>();

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable playable) {
            IsStateActive = true;
            ObserveStateEnter.OnNext(new FSMUnit {
                StateInfo = stateInfo,
                LayerIndex = layerIndex,
                Playable = playable
            });
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable playable) {
            IsStateActive = false;
            ObserveStateExit.OnNext(new FSMUnit {
                StateInfo = stateInfo,
                LayerIndex = layerIndex,
                Playable = playable
            });
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable playable) {
            ObserveStateUpdate.OnNext(new FSMUnit {
                StateInfo = stateInfo,
                LayerIndex = layerIndex,
                Playable = playable
            });
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