using UnityEngine;
using Redux;

using Banchou.DependencyInjection;
using Banchou.Pawn;

namespace Banchou.Combatant {
    public class CombatantContext : MonoBehaviour, IContext {
        private PawnId _pawnId;
        private CombatantActions _combatantActions;

        public void Construct(
            PawnId pawnId,
            GetState getState,
            Dispatcher dispatch,
            CombatantActions combatantActions
        ) {
            _pawnId = pawnId;
            _combatantActions = combatantActions;

            if (getState().GetCombatant(_pawnId) == null) {
                dispatch(_combatantActions.Add(_pawnId));
            }
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<CombatantActions>(_combatantActions);
        }
    }
}