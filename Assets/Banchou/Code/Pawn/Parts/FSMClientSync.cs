using System;
using UniRx;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Pawn.Part {
    /// <summary>
    /// If the the current game is on a network client, synchronizes this Pawn's animator with FSMStateChanged actions
    /// delivered over the network.
    ///
    /// Probably don't want to handle it like this. The server should be sending inputs to all clients continuously, and they
    /// should all be running their own FSMs + rollback, since we don't want to explicitly synchronize animation parameters.
    /// Need to figure out the time syncing to make this work.
    ///
    /// We'll periodically sync animation state, maybe through this?
    ///
    /// Animators on both client and server both need to respond to redux changes as well as input. With both sent over the line,
    /// clients <i>should</i> be able to simulate accurately, albeit at a delay.
    ///
    /// If we avoid using FSMStateChange, we'll need to rely on SyncPawn to update the transform, and the inputs+actions+rollback to update
    /// the state. Actions aren't equipped for rollback, but maybe they can be?
    ///
    /// Need to generalize rollback by checking the server timestamp of every state change in every state machine behaviour?
    /// </summary>
    public class FSMClientSync : MonoBehaviour {
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> onStateUpdate,
            Animator animator = null
        ) {
            if (animator != null) {
                onStateUpdate
                    .Where(state => state.IsClient())
                    .Select(state => state.GetLatestFSMChange())
                    .DistinctUntilChanged()
                    .Where(fsmChange => fsmChange.PawnId == pawnId)
                    .CatchIgnoreLog()
                    .Subscribe(fsmChange => {
                        animator.Play(fsmChange.StateHash, 0, 0f);
                    })
                    .AddTo(this);
            }
        }
    }
}