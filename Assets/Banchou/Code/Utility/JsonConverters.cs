using System;
using System.Net;

using Newtonsoft.Json;

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
            var id = reader.ReadAsInt32() ?? -1;
            return new PlayerId(id);
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
            var id = reader.ReadAsInt32() ?? -1;
            return new PawnId(id);
        }

        public override void WriteJson(JsonWriter writer, PawnId value, JsonSerializer serializer) {
            serializer.Serialize(writer, value.Id);
        }
    }

    public class IPEndPointConverter : JsonConverter<IPEndPoint> {
        public override IPEndPoint ReadJson(JsonReader reader, Type objectType, IPEndPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var str = reader.ReadAsString();
            var ipAndPort = str.Split(':');

            IPAddress ip;
            if (ipAndPort.Length > 0 && IPAddress.TryParse(ipAndPort[0], out ip)) {
                int port;
                if (ipAndPort.Length > 1 && int.TryParse(ipAndPort[1], out port)) {
                    return new IPEndPoint(ip, port);
                }
            };
            throw new JsonSerializationException($"Could not read IPEndPoint from {str}");
        }

        public override void WriteJson(JsonWriter writer, IPEndPoint value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.Address.ToString()}:{value.Port}");
        }
    }
}