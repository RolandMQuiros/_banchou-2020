using UnityEngine;

using MessagePack;
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

    [MessagePackObject]
    public struct SyncClient {
        [Key(0)] public PlayerId PlayerId;
        [Key(1)] public GameState GameState;
    }

    [MessagePackObject]
    public struct PlayerMove {
        [Key(0)] public PlayerId PlayerId;
        [Key(1)] public Vector3 Direction;
    }

    [MessagePackObject]
    public struct PlayerCommand {
        [Key(0)] public PlayerId PlayerId;
        [Key(1)] public InputCommand Command;
    }

    [MessagePackObject]
    public struct SyncPawn {
        [Key(0)] public PawnId PawnId;
        [Key(1)] public Vector3 Position;
        [Key(2)] public Quaternion Rotation;
    }

    public delegate void PullPawnSync(SyncPawn syncPawn);
    public delegate void PushPawnSync(SyncPawn syncPawn);
}