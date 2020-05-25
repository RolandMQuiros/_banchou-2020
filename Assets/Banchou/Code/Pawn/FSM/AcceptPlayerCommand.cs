using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;
using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class AcceptPlayerCommand : FSMBehaviour {
        [SerializeField] private Command _acceptedCommand = Command.None;
        [SerializeField, Range(0f, 1f)] private float _fromStateTime = 0f;
        [SerializeField, Range(0f, 1f)] private float _toStateTime = 1f;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            PawnId pawnId,
            Animator animator
        ) {
            var enterTime = Time.fixedTime;
            var exitTime = Time.fixedTime;
            var commandHash = Animator.StringToHash($"C:{_acceptedCommand}");

            var observeCommand = observeState
                .Select(state => state.GetCombatantLastCommand(pawnId))
                .DistinctUntilChanged()
                .Where(_ => getState().IsPawnSelected(pawnId))
                .Where(lastCommand => lastCommand.Command != Command.None && lastCommand.Command == _acceptedCommand)
                .Select(lastCommand => lastCommand.Command);

            var wasTriggered = false;

            observeCommand
                .Where(_ => IsStateActive && !wasTriggered)
                .WithLatestFrom(
                    ObserveStateUpdate,
                    (command, stateInfo) => stateInfo.loop ? stateInfo.normalizedTime % 1 : stateInfo.normalizedTime
                )
                .Where(stateTime => stateTime >= _fromStateTime && stateTime <= _toStateTime)
                .Subscribe(_ => {
                    wasTriggered = true;
                    animator.SetTrigger(commandHash);
                })
                .AddTo(Streams);

            ObserveStateEnter
                .Subscribe(_ => {
                    if (wasTriggered) {
                        animator.ResetTrigger(commandHash);
                        wasTriggered = false;
                    }
                })
                .AddTo(Streams);
        }
    }
}