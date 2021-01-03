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
        PlayerInput,
        SyncPawn,
        ServerTimeRequest,
        ServerTimeResponse
    }

    [MessagePackObject]
    public struct Envelope {
        [Key(0)] public PayloadType PayloadType;
        [Key(1)] public byte[] Payload;

        public static byte[] CreateMessage<T>(PayloadType payloadType, T payload, MessagePackSerializerOptions options) {
            return MessagePackSerializer.Serialize(
                new Envelope {
                    PayloadType = payloadType,
                    Payload = MessagePackSerializer.Serialize(payload, options)
                },
                options
            );
        }
    }

    [MessagePackObject]
    public struct ConnectClient {
        [Key(0)] public string ConnectionKey;
        [Key(1)] public float ClientConnectionTime;
    }

    [MessagePackObject]
    public struct ReduxAction {
        [Key(0)] public byte[] ActionBytes;
    }

    [MessagePackObject]
    public struct SyncClient {
        [Key(0)] public Guid ClientNetworkId;
        [Key(1)] public byte[] GameStateBytes;
        [Key(2)] public float ClientTime;
        [Key(3)] public float ServerTime;
    }

    [MessagePackObject]
    public struct SyncPawn {
        [Key(0)] public PawnId PawnId;
        [Key(1)] public Vector3 Position;
        [Key(2)] public Vector3 Forward;
        [Key(3)] public float When;
    }

    // https://gamedev.stackexchange.com/questions/93477/how-to-keep-server-client-clocks-in-sync-for-precision-networked-games-like-quak
    [MessagePackObject]
    public struct ServerTimeRequest {
        [Key(0)] public float ClientTime;
    }

    [MessagePackObject]
    public struct ServerTimeResponse {
        [Key(0)] public float ClientTime;
        [Key(1)] public float ServerTime;
    }

    public delegate void PullPawnSync(SyncPawn syncPawn);
    public delegate void PushPawnSync(SyncPawn syncPawn);
}