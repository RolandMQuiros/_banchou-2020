using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

using Banchou.Network;
using Banchou.Network.Message;
using Banchou.Player;

namespace Banchou.Pawn.Part {
    public class PawnSync : MonoBehaviour {
        [SerializeField] private int _frequency = 20;
        public void Construct(
            PawnId pawnId,
            IPawnInstance pawn,
            IObservable<GameState> onStateUpdate,
            ObservePlayerMove onPlayerMove,
            PushPawnSync pushPawnSync,
            GetServerTime getServerTime
        ) {
            onStateUpdate
                .Select(state => state.IsServer())
                .DistinctUntilChanged()
                .Where(isServer => isServer)
                .SelectMany(_ =>
                    // Sync at a certain frequency
                    this.FixedUpdateAsObservable()
                        .SampleFrame(_frequency, FrameCountType.Update)
                        // Sync when movement direction changes
                        .Merge(onPlayerMove().DistinctUntilChanged().Select(__ => new Unit()))
                        .StartWith(new Unit())
                )
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    pushPawnSync(
                        new SyncPawn {
                            PawnId = pawnId,
                            Position = pawn.Position,
                            Forward = pawn.Forward,
                            When = getServerTime()
                        }
                    );
                })
                .AddTo(this);
        }
    }

}