using UnityEngine;

using Banchou.Combatant;
using Banchou.DependencyInjection;
using Banchou.Mob;
using Banchou.Network;
using Banchou.Pawn;

namespace Banchou.Board {
    public class BoardContext : MonoBehaviour, IContext {
        private PawnFactory _pawnFactory = null;
        private BoardActions _boardActions;
        private MobActions _mobActions;
        private CombatantActions _combatantActions;

        public void Construct(GetTime getTime) {
            _boardActions = new BoardActions(getTime);
            _mobActions = new MobActions(getTime);
            _combatantActions = new CombatantActions(getTime);
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<BoardActions>(_boardActions);
            container.Bind<MobActions>(_mobActions);
            container.Bind<CombatantActions>(_combatantActions);

            _pawnFactory = _pawnFactory ?? GetComponentInChildren<PawnFactory>();
            container.Bind<IPawnInstances>(_pawnFactory);
        }
    }
}