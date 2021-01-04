using System;
using System.Collections.Generic;

using Banchou.Board;
using Banchou.Player;

namespace Banchou.Network {
    public struct RollbackUnit {
        public IList<InputUnit> InputUnits;
        public BoardState TargetState;
        public float When;
        public float CorrectionTime;
        public float DeltaTime;
    }

    public interface IRollbackEvents {
        IObservable<RollbackUnit> OnResimulationStart { get; }
        IObservable<RollbackUnit> BeforeResimulateStep { get; }
        IObservable<RollbackUnit> AfterResimulateStep { get; }
        IObservable<RollbackUnit> OnResimulationEnd { get; }
    }
}