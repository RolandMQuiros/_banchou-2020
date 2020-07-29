using System.Collections;
using System.Collections.Generic;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;

namespace Banchou.Pawn.FSM {
    public class AcceptCommandToBool : FSMBehaviour {
        [SerializeField] private InputCommand _enableCommand = InputCommand.LockOn;
        [SerializeField] private InputCommand _disableCommand = InputCommand.LockOff;
        [SerializeField] private string _outputParameter = string.Empty;

        public void Construct(
            Animator stateMachine,
            ObservePlayerCommand observeCommands,
            Dispatcher dispatch
        ) {
            var outputHash = Animator.StringToHash(_outputParameter);

            observeCommands()
                .Subscribe(command => {
                    if (command == _enableCommand) {
                        stateMachine.SetBool(outputHash, true);
                    } else if (command == _disableCommand) {
                        stateMachine.SetBool(outputHash, false);
                    }
                })
                .AddTo(Streams);
        }
    }
}