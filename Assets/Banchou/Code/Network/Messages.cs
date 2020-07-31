using UnityEngine;

using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Network.Message {
    public enum PayloadType : byte {
        Action,
        SyncClient,
        PlayerMove,
        PlayerCommand,
        SyncPawn,
    }

    public struct Envelope {
        public PayloadType PayloadType;
        public object Payload;
    }

    public struct SyncClient {
        public PlayerId PlayerId;
        public GameState GameState;
    }

    public struct PlayerMove {
        public PlayerId PlayerId;
        public Vector3 Direction;
    }

    public struct PlayerCommand {
        public PlayerId PlayerId;
        public InputCommand Command;
    }

    public struct SyncPawn {
        public PawnId PawnId;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public delegate void PullPawnSync(SyncPawn syncPawn);
    public delegate void PushPawnSync(SyncPawn syncPawn);
}