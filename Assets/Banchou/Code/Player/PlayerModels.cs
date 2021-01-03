using System;
using System.Collections.Generic;
using System.ComponentModel;

using MessagePack;
using UnityEngine;

using Banchou.Pawn;
using Banchou.Utility;

namespace Banchou.Player {
    [TypeConverter(typeof(PlayerIdTypeConverter)), MessagePackObject]
    public struct PlayerId {
        public static readonly PlayerId Empty = new PlayerId();
        private static int _idCounter = 1;
        [Key(0)] public int Id { get; private set; }

        public static PlayerId Create() {
            return new PlayerId {
                Id = _idCounter++
            };
        }

        public PlayerId(int id) {
            Id = id;
        }

        #region Equality boilerplate
        public override bool Equals(object obj) => GetType() == obj.GetType() && Id == ((PlayerId)obj).Id;
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PlayerId first, PlayerId second) => first.Equals(second);
        public static bool operator!=(PlayerId first, PlayerId second) => !first.Equals(second);
        #endregion
    }

    public class PlayerState {
        public string PrefabKey = string.Empty;
        public string Name = null;
        public Guid NetworkId = Guid.Empty;
        public PawnId Pawn = PawnId.Empty;
        public bool RollbackEnabled = true;

        public PlayerState() { }
        public PlayerState(in PlayerState prev) {
            PrefabKey = prev.PrefabKey;
            Name = prev.Name;
            NetworkId = prev.NetworkId;
            Pawn = prev.Pawn;
            RollbackEnabled = prev.RollbackEnabled;
        }
    }

    public class PlayersState {
        public Dictionary<PlayerId, PlayerState> States = new Dictionary<PlayerId, PlayerState>();

        public PlayersState() { }
        public PlayersState(in PlayersState prev) {
            States = prev.States;
        }
    }

    public enum InputUnitType : byte {
        Command,
        Movement,
        Look
    }

    [MessagePackObject]
    public struct InputUnit {
        [Key(0)] public InputUnitType Type;
        [Key(1)] public PlayerId PlayerId;
        [Key(2)] public InputCommand Command;
        [Key(3)] public Vector3 Direction;
        [Key(4)] public float When;
        public Vector2 Look => new Vector2(Direction.x, Direction.y);

        public InputUnit(in InputUnit prev) {
            Type = prev.Type;
            PlayerId = prev.PlayerId;
            Command = prev.Command;
            Direction = prev.Direction;
            When = prev.When;
        }
    }
}