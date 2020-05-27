﻿using System;
using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Redux;

using Banchou.Pawn;
using Banchou.Pawn.Part;

namespace Banchou.Player.Targeting {
    [RequireComponent(typeof(Collider))]
    public class TargetingVolume : MonoBehaviour {
        public void Construct(
            PlayerId playerId,
            GetState getState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            IPawnInstances pawnInstances,
            IObservable<GameState> observeState
        ) {
            this.OnTriggerEnterAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable != null && getState().GetPlayerPawn(playerId) != targetable.PawnId) {
                        dispatch(playerActions.AddTarget(playerId, targetable.PawnId));
                    }
                })
                .AddTo(this);

            this.OnTriggerExitAsObservable()
                .Subscribe(collider => {
                    var targetable = collider.GetComponent<Targetable>();
                    if (targetable != null && getState().GetPlayerPawn(playerId) != targetable.PawnId) {
                        dispatch(playerActions.RemoveTarget(playerId, targetable.PawnId));
                    }
                })
                .AddTo(this);
        }
    }
}