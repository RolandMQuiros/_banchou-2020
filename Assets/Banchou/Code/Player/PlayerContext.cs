using System;
using UnityEngine;
using UniRx;

using Redux;
using Banchou.Pawn;

namespace Banchou.Player {
    public delegate void PushMove(Vector3 direction);
    public delegate void PushLook(Vector2 direction);

    public class PlayerContext : MonoBehaviour, IContext {
        public PlayerId PlayerId { get; set; } = PlayerId.Create();

        [SerializeField] private InputSource _playerInputSource = InputSource.AI;

        private GetState _getState;
        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private PlayerActions _playerActions;
        private IPlayerInstances _playerInstances;
        private PlayerInputStreams _playerInputStreams;

        public void Construct(
            GetState getState,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            PlayerActions playerActions,
            IPlayerInstances playerInstances,
            IPawnInstances pawnInstances,
            PlayerInputStreams playerInputStreams
        ) {
            _getState = getState;
            _observeState = observeState;
            _dispatch = dispatch;
            _playerActions = playerActions;
            _playerInstances = playerInstances;
            _playerInputStreams = playerInputStreams;
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PlayerId>(PlayerId);
            container.Bind<PushMove>(direction => { _playerInputStreams.PushMove(PlayerId, direction); });
            container.Bind<PushLook>(direction => { _playerInputStreams.PushLook(PlayerId, direction); });

            container.Bind<ObservePlayerLook>(
                () => _playerInputStreams
                    .ObserveLook(PlayerId)
                    .WithLatestFrom(
                        _observeState
                            .Select(state => state.GetPlayer(PlayerId))
                            .DistinctUntilChanged(),
                        (look, player) => (look, player)
                    )
                    .Where(t => t.player?.Source == InputSource.LocalSingle || t.player?.Source == InputSource.LocalMulti)
                    .Select(t => t.look)
            );

            container.Bind<ObservePlayerMove>(
                () => _playerInputStreams
                    .ObserveMove(PlayerId)
                    .WithLatestFrom(
                        _observeState
                            .Select(state => state.GetPlayer(PlayerId))
                            .DistinctUntilChanged(),
                        (move, player) => (move, player)
                    )
                    .Where(t => t.player?.Source == InputSource.LocalSingle || t.player?.Source == InputSource.LocalMulti)
                    .Select(t => t.move)
            );
        }

        private void Start() {
            var state = _getState();
            var player = state.GetPlayer(PlayerId);
            if (player == null) {
                _playerInstances.Set(PlayerId, gameObject);
                _dispatch(_playerActions.Add(PlayerId, _playerInputSource));
            }
        }
    }

}