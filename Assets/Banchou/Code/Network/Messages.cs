using System;
using UnityEngine;

using MessagePack;
using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Network.Message {
    public enum PayloadType : byte {
        ConnectClient,
        ReduxAction,
        SyncClient,
        PlayerMove,
        PlayerCommand,
        SyncPawn,
    }

    [MessagePackObject]
    public struct Envelope {
        [Key(0)] public PayloadType PayloadType;
        [Key(1)] public byte[] Payload;

        public static byte[] CreateMessage(PayloadType payloadType, object payload, MessagePackSerializerOptions options) {
            return MessagePackSerializer.Serialize(
                new Envelope {
                    PayloadType = PayloadType.ConnectClient,
                    Payload = MessagePackSerializer.Serialize(payload, options)
                },
                options
            );
        }
    }

    [MessagePackObject]
    public struct ConnectClient {
        [Key(0)] public Guid ClientNetworkId;
    }

    [MessagePackObject]
    public struct ReduxAction {
        [Key(0)] public byte[] ActionBytes;
    }

    [MessagePackObject]
    public struct SyncClient {
        [Key(0)] public byte[] GameStateBytes;
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