using System;
using UnityEngine;
using UniRx;

namespace Banchou.Player {
    public delegate IObservable<Vector3> ObservePlayerMove();
    public delegate IObservable<Vector2> ObservePlayerLook();
    public delegate IObservable<InputCommand> ObservePlayerCommand();

    public class PlayerInputStreams : IObservable<InputUnit> {
        private Subject<InputUnit> _inputSubject = new Subject<InputUnit>();

        public PlayerInputStreams Push(InputUnit inputUnit) {
            _inputSubject.OnNext(inputUnit);
            return this;
        }

        public PlayerInputStreams PushMove(PlayerId playerId, Vector3 move, float when) {
            _inputSubject.OnNext(new InputUnit {
                Type = InputUnitType.Movement,
                PlayerId = playerId,
                Direction = move,
                When = when
            });
            return this;
        }

        public PlayerInputStreams PushLook(PlayerId playerId, Vector3 look, float when) {
            _inputSubject.OnNext(new InputUnit {
                Type = InputUnitType.Look,
                PlayerId = playerId,
                Direction = look,
                When = when
            });
            return this;
        }

        public PlayerInputStreams PushCommand(PlayerId playerId, InputCommand command, float when) {
            _inputSubject.OnNext(new InputUnit {
                Type = InputUnitType.Command,
                PlayerId = playerId,
                Command = command,
                When = when
            });
            return this;
        }

        public IDisposable Subscribe(IObserver<InputUnit> observer) {
            return ((IObservable<InputUnit>)_inputSubject).Subscribe(observer);
        }
    }

    public static class InputStreamExtensions {
        public static IObservable<InputUnit> ObserveCommands(this IObservable<InputUnit> source) {
            return source.Where(unit => unit.Type == InputUnitType.Command);
        }

        public static IObservable<InputUnit> ObserveCommands(this IObservable<InputUnit> source, PlayerId playerId) {
            return source.ObserveCommands().Where(unit => unit.PlayerId == playerId);
        }

        public static IObservable<InputUnit> ObserveMoves(this IObservable<InputUnit> source) {
            return source.Where(unit => unit.Type == InputUnitType.Movement)
                .DistinctUntilChanged();
        }

        public static IObservable<InputUnit> ObserveMoves(this IObservable<InputUnit> source, PlayerId playerId) {
            return source.ObserveMoves().Where(unit => unit.PlayerId == playerId);
        }

        public static IObservable<InputUnit> ObserveLook(this IObservable<InputUnit> source) {
            return source.Where(unit => unit.Type == InputUnitType.Look)
                .DistinctUntilChanged();
        }

        public static IObservable<InputUnit> ObserveLook(this IObservable<InputUnit> source, PlayerId playerId) {
            return source.ObserveLook().Where(unit => unit.PlayerId == playerId);
        }
    }
}
