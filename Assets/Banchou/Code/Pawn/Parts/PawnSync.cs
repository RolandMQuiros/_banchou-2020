using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

using Banchou.Network.Message;
using Banchou.Player;

namespace Banchou.Pawn.Part {
    public class PawnSync : MonoBehaviour {
        [SerializeField] private int _frequency = 20;
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> onStateUpdate,
            ObservePlayerMove onPlayerMove,
            PushPawnSync pushPawnSync
        ) {
            var pawn = GetComponent<IPawnInstance>();

            onStateUpdate
                .Select(state => state.Network.Id)
                .Where(networkId => networkId == default)
                .DistinctUntilChanged()
                .SelectMany(_ =>
                    // Sync at a certain frequency
                    this.FixedUpdateAsObservable()
                        .SampleFrame(_frequency, FrameCountType.Update)
                        // Sync when movement direction changes
                        .Merge(onPlayerMove().DistinctUntilChanged().Select(_ => new Unit()))
                )
                .Subscribe(_ => {
                    pushPawnSync(
                        new SyncPawn {
                            PawnId = pawnId,
                            Position = pawn.Position,
                            Forward = pawn.Forward
                        }
                    );
                })
                .AddTo(this);
        }
    }

}