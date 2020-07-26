using System.Runtime.InteropServices;
using UnityEngine;

using Banchou.Player;
using Banchou.Pawn;

namespace Banchou.Network.Message {
    public enum PayloadType : byte {
        Action,
        PlayerConnected,
        PlayerMove,
        PlayerCommand,
        SyncPawn,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Envelope {
        public PayloadType PayloadType;
        public object Payload;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlayerConnected {
        public PlayerId PlayerId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlayerMove {
        public PlayerId PlayerId;
        public Vector3 Direction;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PlayerCommand {
        public PlayerId PlayerId;
        public InputCommand Command;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SyncPawn {
        public PawnId PawnId;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public delegate void PushPawnSync(SyncPawn syncPawn);
}