namespace Banchou.Pawn {
    namespace StateAction {
        public class PawnAction {
            public PawnId PawnId;
        }

        public class FSMStateChanged : PawnAction {
            public int Statehash;
            public bool IsLoop;
            public float ClipLength;
            public float When;
        }

        public class RollbackStarted : PawnAction { }
        public class FastForwarding : PawnAction {
            public float CorrectionTime;
        }
        public class RollbackComplete : PawnAction { }
    }

    public class PawnActions {
        private PawnId _pawnId;

        public PawnActions(PawnId pawnId) {
            _pawnId = pawnId;
        }

        public StateAction.FSMStateChanged FSMStateChanged(int stateNameHash, float clipLength, bool isLoop, float when) => new StateAction.FSMStateChanged {
            PawnId = _pawnId,
            Statehash = stateNameHash,
            IsLoop = isLoop,
            ClipLength = clipLength,
            When = when
        };

        public StateAction.RollbackStarted RollbackStarted() => new StateAction.RollbackStarted { PawnId = _pawnId };
        public StateAction.FastForwarding FastForwarding(float correctionTime) => new StateAction.FastForwarding {
            PawnId = _pawnId,
            CorrectionTime = correctionTime
        };
        public StateAction.RollbackComplete RollbackComplete() => new StateAction.RollbackComplete { PawnId = _pawnId };
    }
}