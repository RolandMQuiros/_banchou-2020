using System;
using System.Collections.Generic;
using System.ComponentModel;

using MessagePack;
using UnityEngine;

using Banchou.Player;
using Banchou.Utility;

namespace Banchou.Pawn {
    [TypeConverter(typeof(PawnIdTypeConverter)), MessagePackObject]
    public struct PawnId {
        public static readonly PawnId Empty = new PawnId();
        private static int _idCounter = 1;
        [Key(0)] public int Id { get; private set; }

        public static PawnId Create() {
            return new PawnId {
                Id = _idCounter++
            };
        }

        public PawnId(int id) {
            Id = id;
        }

        #region Equality boilerplate
        public override bool Equals(object obj) => GetType() == obj.GetType() && Id.Equals(((PawnId)obj).Id);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => Id.ToString();
        public static bool operator==(PawnId first, PawnId second) => first.Equals(second);
        public static bool operator!=(PawnId first, PawnId second) => !first.Equals(second);
        #endregion
    }

    public class PawnState {
        public PlayerId PlayerId;
        public string PrefabKey = string.Empty;
        public float TimeScale = 1f;

        public Vector3 SpawnPosition = Vector3.zero;
        public Quaternion SpawnRotation = Quaternion.identity;

        public PawnState() { }
        public PawnState(in PawnState prev) {
            PlayerId = prev.PlayerId;
            PrefabKey = prev.PrefabKey;
            TimeScale = prev.TimeScale;
            SpawnPosition = prev.SpawnPosition;
            SpawnRotation = prev.SpawnRotation;
        }
    }

    public class PawnsState {
        public Dictionary<PawnId, PawnState> States = new Dictionary<PawnId, PawnState>();
        public float LastUpdated = 0f;

        public PawnsState() { }
        public PawnsState(in PawnsState prev) {
            States = prev.States;
            LastUpdated = prev.LastUpdated;
        }
    }
}