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
            );

            container.Bind<ObservePlayerMove>(
                () => _playerInputStreams
                    .ObserveMoves(PlayerId)
                    .Select(moveUnit => moveUnit.Direction)
            );
        }
    }

}