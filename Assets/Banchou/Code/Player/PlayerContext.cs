using System;

using UniRx;
using UnityEngine;

using Banchou.DependencyInjection;

namespace Banchou.Player {
    public class PlayerContext : MonoBehaviour, IContext {
        public PlayerId PlayerId { get; private set; }
        private IObservable<GameState> _observeState;
        private PlayerInputStreams _playerInputStreams;

        public void Construct(
            PlayerId playerId,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInputStreams
        ) {
            PlayerId = playerId;
            _observeState = observeState;
            _playerInputStreams = playerInputStreams;
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PlayerId>(PlayerId);
            container.Bind<ObservePlayerLook>(
                () => _playerInputStreams
                    .ObserveLook(PlayerId)
                    .Select(lookUnit => lookUnit.Look)
                    .WithLatestFrom(
                        _observeState
                            .Select(state => state.GetPlayer(PlayerId))
                            .DistinctUntilChanged(),
                        (look, player) => (look, player)
                    )
                    .Select(t => t.look)
            );

            container.Bind<ObservePlayerMove>(
                () => _playerInputStreams
                    .ObserveMove(PlayerId)
                    .Select(moveUnit => moveUnit.Move)
                    .WithLatestFrom(
                        _observeState
                            .Select(state => state.GetPlayer(PlayerId))
                            .DistinctUntilChanged(),
                        (move, player) => (move, player)
                    )
                    .Select(t => t.move)
            );
        }
    }

}