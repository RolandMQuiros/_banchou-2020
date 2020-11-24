using System;
using UnityEngine;
using UniRx;

using Banchou.Network.Message;
using UniRx.Triggers;

namespace Banchou.Pawn.Part {
    public class PawnSync : MonoBehaviour {
        [SerializeField] private int _frequency = 20;
        public void Construct(
            PawnId pawnId,
            IObservable<GameState> onStateUpdate,
            PushPawnSync pushPawnSync
        ) {
            var pawn = GetComponent<IPawnInstance>();

            onStateUpdate
                .Select(state => state.Network.Id)
                .Where(networkId => networkId == default)
                .DistinctUntilChanged()
                .SelectMany(_ => this.UpdateAsObservable())
                .SampleFrame(_frequency, FrameCountType.Update)
                .Subscribe(_ => {
                    pushPawnSync(
                        new SyncPawn {
                            PawnId = pawnId,
                            Position = pawn.Position,
                            Forward = pawn.Forward
                        }
                    );
                });
        }
    }

}