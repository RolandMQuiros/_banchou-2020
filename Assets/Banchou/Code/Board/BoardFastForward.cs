using System;
using System.Collections.Generic;

using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Banchou.Board {
    public class BoardFastForward : MonoBehaviour {
        private struct HistoryStep {
            public GameState State;
            public float When;
        }

        public void Construct(IObservable<GameState> observeState) {
            var history = new LinkedList<HistoryStep>();

        }
    }
}