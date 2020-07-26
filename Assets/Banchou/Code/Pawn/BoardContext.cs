using System;
using UnityEngine;

using Banchou.Mob;
using Banchou.Combatant;

namespace Banchou.Pawn {
    public class BoardContext : MonoBehaviour, IContext {
        [SerializeField] private Transform _pawnParent = null;
        [SerializeField] private PawnFactory _pawnFactory = null;

        private BoardActions _boardActions = new BoardActions();
        private MobActions _mobActions;
        private CombatantActions _combatantActions;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Instantiator instantiate
        ) {
            _pawnFactory.Construct(_pawnParent ?? transform, observeState, getState, instantiate);

            _boardActions = new BoardActions();
            _mobActions = new MobActions();
            _combatantActions = new CombatantActions(_boardActions);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<BoardActions>(_boardActions);
            container.Bind<MobActions>(_mobActions);
            container.Bind<CombatantActions>(_combatantActions);
            container.Bind<IPawnInstances>(_pawnFactory);
        }
    }
}