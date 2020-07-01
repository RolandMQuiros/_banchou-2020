using System;
using UnityEngine;
using UniRx;

using Banchou.Combatant;
using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class AcceptPlayerCommand : FSMBehaviour {
        [SerializeField] private Command _acceptedCommand = Command.None;

        [SerializeField, Range(0f, 1f), Tooltip("The normalized state time after which the command is accepted")]
        private float _acceptFromStateTime = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("The normalized state time after which the command is no longer accepted")]
        private float _acceptUntilStateTime = 1f;

        [SerializeField, Range(0f, 1f), Tooltip("When, in regular state time, the accepted command is output to a trigger")]
        private float _bufferUntilStateTime = 0f;

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
                .Where(lastCommand => lastCommand.Command != Command.None && lastCommand.Command == _acceptedCommand)
                .Select(lastCommand => lastCommand.Command);

            var wasTriggered = false;

            observeCommand
                .Where(_ => IsStateActive && !wasTriggered)
                .WithLatestFrom(
                    ObserveStateUpdate,
                    (command, stateInfo) => stateInfo.normalizedTime % 1
                )
                .Where(stateTime => stateTime >= _acceptFromStateTime && stateTime <= _acceptUntilStateTime)
                .Subscribe(_ => { wasTriggered = true; })
                .AddTo(Streams);

            ObserveStateUpdate
                .Where(stateInfo => wasTriggered && stateInfo.normalizedTime >= _bufferUntilStateTime)
                .Subscribe(_ => { animator.SetTrigger(commandHash); })
                .AddTo(Streams);

            ObserveStateExit
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