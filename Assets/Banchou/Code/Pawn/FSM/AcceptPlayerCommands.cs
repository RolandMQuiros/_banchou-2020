using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

using Banchou.Player;
using Banchou.Combatant;

namespace Banchou.Pawn.FSM {
    public class AcceptPlayerCommands : FSMBehaviour {
        [SerializeField] private Command[] _acceptedCommands = null;

        public void Construct(
            IObservable<GameState> observeState,
            PawnId pawnId,
            Animator animator
        ) {
            var accepted = new HashSet<Command>(_acceptedCommands);
            var observeCommands = observeState
                    .Select(state => state.GetCombatantLastCommand(pawnId))
                    .Where(lastCommand => lastCommand.Command != Command.None)
                    .DistinctUntilChanged()
                    .Select(pushed => pushed.Command);
            var triggered = false;

            observeCommands
                .Where(command => IsStateActive && !triggered && accepted.Contains(command))
                .Subscribe(command => {
                    animator.SetTrigger($"C:{command}");
                    triggered = true;
                })
                .AddTo(Streams);

            ObserveStateExit
                .Subscribe(_ => {
                    triggered = false;
                    foreach (var command in accepted) {
                        animator.ResetTrigger($"C:{command}");
                    }
                })
                .AddTo(Streams);
        }
    }
}