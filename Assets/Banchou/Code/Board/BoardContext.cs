﻿using UnityEngine;

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

        public void Construct(GetServerTime getServerTime) {
            _boardActions = new BoardActions(getServerTime);
            _mobActions = new MobActions(getServerTime);
            _combatantActions = new CombatantActions(getServerTime);
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