using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;

namespace Banchou.Pawn.FSM {
    public class HitLag : FSMBehaviour {
        [SerializeField] private float _delay = 0.1f;
        [SerializeField] private float _lagSpeed = 0f;
        [SerializeField] private float _normalSpeed = 1f;

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            Animator animator
        ) {
            var observeHits = observeState
                .Select(state => state.GetCombatantHitDealt(pawnId))
                .DistinctUntilChanged()
                .Where(dealt => dealt?.Medium == HitMedium.Melee && IsStateActive);

            observeHits
                .Subscribe(_ => { animator.speed = _lagSpeed; })
                .AddTo(Streams);

            observeHits
                .Delay(TimeSpan.FromSeconds(_delay))
                .Subscribe(_ => { animator.speed = _normalSpeed; })
                .AddTo(Streams);
        }
    }
}