using System;
using System.Linq;

using Redux;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

using Banchou.Combatant;
using Banchou.DependencyInjection;
using Banchou.Mob;
using Banchou.Pawn;

namespace Banchou.Board {
    public class BoardContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _pawnParent = null;
        [SerializeField] private PawnFactory _pawnFactory = null;

        private BoardActions _boardActions;
        private MobActions _mobActions;
        private CombatantActions _combatantActions;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            _pawnFactory?.Construct(_pawnParent ?? transform, observeState, getState, instantiate);

            _boardActions = new BoardActions();
            _mobActions = new MobActions();
            _combatantActions = new CombatantActions();
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<BoardActions>(_boardActions);
            container.Bind<MobActions>(_mobActions);
            container.Bind<CombatantActions>(_combatantActions);
            container.Bind<IPawnInstances>(_pawnFactory);
        }
    }
}