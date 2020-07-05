using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Banchou.Pawn;
using Banchou.Player;

namespace Banchou.Utility {
    public class PlayerIdConverter : JsonConverter<PlayerId> {
        public override PlayerId ReadJson(
            JsonReader reader,
            Type objectType,
            PlayerId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        ) {
            var obj = JObject.ReadFrom(reader);
            var guid = obj.ToObject<Guid>();
            return new PlayerId { Id = guid };
        }

        public override void WriteJson(JsonWriter writer, PlayerId value, JsonSerializer serializer) {
            serializer.Serialize(writer, value.Id);
        }
    }

    public class PawnIdConverter : JsonConverter<PawnId> {
        public override PawnId ReadJson(
            JsonReader reader,
            Type objectType,
            PawnId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        ) {
            var obj = JObject.ReadFrom(reader);
            var guid = obj.ToObject<Guid>();
            return new PawnId { Id = guid };
        }

        public override void WriteJson(JsonWriter writer, PawnId value, JsonSerializer serializer) {
            serializer.Serialize(writer, value.Id);
        }
    }
}