using System;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Combatant;

namespace Banchou.Pawn.FSM {
    public class AcceptLockOnCommands : FSMBehaviour {
        public void Construct(
            IObservable<InputCommand> observeCommands
        ) {
            observeCommands
                // .Select(command => command == InputCommand.LockOn)
                .DistinctUntilChanged()
                .Subscribe(_ => {

                })
                .AddTo(Streams);
        }
    }
}