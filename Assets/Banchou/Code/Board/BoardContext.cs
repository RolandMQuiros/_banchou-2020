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
        private PawnFactory _pawnFactory = null;
        private BoardActions _boardActions = new BoardActions();
        private MobActions _mobActions = new MobActions();
        private CombatantActions _combatantActions = new CombatantActions();

        public void InstallBindings(DiContainer container) {
            container.Bind<BoardActions>(_boardActions);
            container.Bind<MobActions>(_mobActions);
            container.Bind<CombatantActions>(_combatantActions);

            _pawnFactory = _pawnFactory ?? GetComponentInChildren<PawnFactory>();
            container.Bind<IPawnInstances>(_pawnFactory);
        }
    }
}