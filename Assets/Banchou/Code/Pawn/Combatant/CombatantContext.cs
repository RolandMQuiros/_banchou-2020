using UnityEngine;
using Redux;

using Banchou.DependencyInjection;
using Banchou.Pawn;

namespace Banchou.Combatant {
    public class CombatantContext : MonoBehaviour, IContext {
        private PawnId _pawnId;
        private GetState _getState;
        private Dispatcher _dispatch;
        private CombatantActions _combatantActions;

        public void Construct(
            PawnId pawnId,
            GetState getState,
            Dispatcher dispatch,
            CombatantActions combatantActions
        ) {
            _pawnId = pawnId;
            _getState = getState;
            _dispatch = dispatch;
            _combatantActions = combatantActions;
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<CombatantActions>(_combatantActions);
        }

        private void Start() {
            if (_getState().GetCombatant(_pawnId) == null) {
                _dispatch(_combatantActions.Add(_pawnId));
            }
        }
    }
}