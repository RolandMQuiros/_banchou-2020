using System;
using UnityEngine;
using UniRx;

namespace Banchou.Player {
    public delegate IObservable<Vector3> ObservePlayerMove();
    public delegate IObservable<Vector2> ObservePlayerLook();
    public delegate IObservable<InputCommand> ObservePlayerCommand();

    public delegate void PushMove(Vector3 direction, float when);
    public delegate void PushLook(Vector2 look, float when);
    public delegate void PushCommand(InputCommand command, float when);

    public class PlayerInputStreams {
        public struct LookUnit {
            public PlayerId PlayerId;
            public Vector2 Look;
            public float When;
        }

        public struct MoveUnit {
            public PlayerId PlayerId;
            public Vector3 Move;
            public float When;
        }

        public struct CommandUnit {
            public PlayerId PlayerId;
            public InputCommand Command;
            public float When;
        }

        private Subject<LookUnit> _lookSubject = new Subject<LookUnit>();
        private Subject<MoveUnit> _moveSubject = new Subject<MoveUnit>();
        private Subject<CommandUnit> _commandSubject = new Subject<CommandUnit>();

        public IObservable<MoveUnit> ObserveMove(PlayerId playerId) {
            return _moveSubject
                .Where(unit => unit.PlayerId == playerId);
        }

        public IObservable<LookUnit> ObserveLook(PlayerId playerId) {
            return _lookSubject
                .Where(unit => unit.PlayerId == playerId);
        }

        public IObservable<CommandUnit> ObserveCommand(PlayerId playerId) {
            return _commandSubject
                .Where(unit => unit.PlayerId == playerId && unit.Command != InputCommand.None);
        }

        public PlayerInputStreams PushMove(PlayerId playerId, Vector3 move, float when) {
            _moveSubject.OnNext(new MoveUnit {
                PlayerId = playerId,
                Move = move,
                When = when
            });
            return this;
        }

        public PlayerInputStreams PushMove(PlayerId playerId, Vector3 move) {
            return PushMove(playerId, move, Time.fixedUnscaledTime);
        }

        public PlayerInputStreams PushLook(PlayerId playerId, Vector3 look, float when) {
            _lookSubject.OnNext(new LookUnit {
                PlayerId = playerId,
                Look = look,
                When = when
            });
            return this;
        }

        public PlayerInputStreams PushLook(PlayerId playerId, Vector3 look) {
            return PushLook(playerId, look, Time.fixedUnscaledTime);
        }

        public PlayerInputStreams PushCommand(PlayerId playerId, InputCommand command, float when) {
            _commandSubject.OnNext(new CommandUnit {
                PlayerId = playerId,
                Command = command,
                When = when
            });
            return this;
        }

        public PlayerInputStreams PushCommand(PlayerId playerId, InputCommand command) {
            return PushCommand(playerId, command, Time.fixedUnscaledTime);
        }
    }
}
