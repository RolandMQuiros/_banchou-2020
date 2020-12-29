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
            Animator animator,
            IObservable<GameState> onStateUpdate,
            ObservePlayerMove onPlayerMove,
            PushPawnSync pushPawnSync,
            GetServerTime getServerTime,

            Rigidbody body,
            Part.Orientation orientation,

            IObservable<SyncPawn> onPawnSync = null
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

            onStateUpdate
                .Select(state => state.GetLatestFSMChange())
                .DistinctUntilChanged()
                .Where(stateChange => stateChange.PawnId == pawnId)
                .CatchIgnoreLog()
                .Subscribe(stateChange => {
                    pawn.Position = stateChange.Position;
                    pawn.Forward = stateChange.Forward;

                    var timeSinceStateStart = getServerTime() - stateChange.When;
                    var targetNormalizedTime = timeSinceStateStart % stateChange.ClipLength;
                    animator.Play(stateChange.StateHash, 0, targetNormalizedTime);
                })
                .AddTo(this);

            // if (onPawnSync != null) {
            //     onPawnSync
            //         .Where(syncPawn => syncPawn.PawnId == pawnId)
            //         .CatchIgnoreLog()
            //         .Subscribe(syncPawn => {
            //             body.transform.position = syncPawn.Position;
            //             if (orientation != null) {
            //                 orientation.transform.rotation = Quaternion.LookRotation(syncPawn.Forward);
            //             } else {
            //                 transform.rotation = Quaternion.LookRotation(syncPawn.Forward);
            //             }
            //         })
            //         .AddTo(this);
            // }
        }
    }

}