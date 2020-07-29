using System;

using Redux;
using UniRx;
using UnityEngine;

using Banchou.Combatant;
using Banchou.DependencyInjection;
using Banchou.Pawn;

namespace Banchou.Player {
    public class PlayerContext : MonoBehaviour, IContext {
        public PlayerId PlayerId { get; private set; }

        private GetState _getState;
        private IObservable<GameState> _observeState;
        private Dispatcher _dispatch;
        private PlayersActions _playerActions;
        private PlayerTargetingActions _targetingActions;
        private IPlayerInstances _playerInstances;
        private PlayerInputStreams _playerInputStreams;

        public void Construct(
            PlayerId playerId,
            GetState getState,
            IObservable<GameState> observeState,
            Dispatcher dispatch,
            CombatantActions combatantActions,
            PlayersActions playersActions,
            IPlayerInstances playerInstances,
            IPawnInstances pawnInstances,
            PlayerInputStreams playerInputStreams
        ) {
            PlayerId = playerId;
            _getState = getState;
            _observeState = observeState;
            _dispatch = dispatch;
            _playerActions = playersActions;
            _playerInstances = playerInstances;
            _playerInputStreams = playerInputStreams;

            _targetingActions = new PlayerTargetingActions(playerId, pawnInstances, combatantActions);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PlayerId>(PlayerId);
            container.Bind<PlayerTargetingActions>(_targetingActions);
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
                    .Where(t => t.player?.Source == InputSource.Local)
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
                    .Where(t => t.player?.Source == InputSource.Local)
                    .Select(t => t.move)
            );
        }
    }

}