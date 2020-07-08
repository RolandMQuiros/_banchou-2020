using System;
using UnityEngine;
using UniRx;

namespace Banchou.Player {
    public delegate IObservable<Vector3> ObservePlayerMove();
    public delegate IObservable<Vector3> ObservePlayerLook();

    public class PlayerInputStreams {
        public struct LookUnit {
            public PlayerId PlayerId;
            public Vector3 Look;
        }

        public struct MoveUnit {
            public PlayerId PlayerId;
            public Vector3 Move;
        }

        private Subject<LookUnit> _lookSubject = new Subject<LookUnit>();
        private Subject<MoveUnit> _moveSubject = new Subject<MoveUnit>();

        public IObservable<Vector3> ObserveMove(PlayerId playerId) {
            return _moveSubject
                .Where(unit => unit.PlayerId == playerId)
                .Select(unit => unit.Move);
        }

        public IObservable<Vector3> ObserveLook(PlayerId playerId) {
            return _lookSubject
                .Where(unit => unit.PlayerId == playerId)
                .Select(unit => unit.Look);
        }

        public PlayerInputStreams PushMove(PlayerId playerId, Vector3 move) {
            _moveSubject.OnNext(new MoveUnit {
                PlayerId = playerId,
                Move = move
            });
            return this;
        }

        public PlayerInputStreams PushLook(PlayerId playerId, Vector3 look) {
            _lookSubject.OnNext(new LookUnit {
                PlayerId = playerId,
                Look = look
            });
            return this;
        }
    }
}
