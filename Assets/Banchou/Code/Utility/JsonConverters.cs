using System;
using System.Net;
using System.ComponentModel;

using MessagePack.Formatters;
using Newtonsoft.Json;

using Banchou.Pawn;
using Banchou.Player;
using MessagePack;

namespace Banchou.Utility {
    public class PlayerIdConverter : JsonConverter<PlayerId> {
        public override PlayerId ReadJson(
            JsonReader reader,
            Type objectType,
            PlayerId existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        ) {
            if (reader.TokenType == JsonToken.Integer) {
                if (reader.Value is int id32) {
                    return new PlayerId(id32);
                } else if (reader.Value is long id64) { // BsonReader is doing some dumb shit
                                                        // where it's storing an int as a long, but only tries to parse ints
                    return new PlayerId((int)id64);
                }
            }
            return new PlayerId(-1);
        }

        public override void WriteJson(JsonWriter writer, PlayerId value, JsonSerializer serializer) {
            writer.WriteValue(value.Id);
        }
    }

    public class PlayerIdTypeConverter : TypeConverter {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            if (value is string str) {
                return new PlayerId(int.Parse(str));
            }
            throw new NotSupportedException();
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
            if (reader.TokenType == JsonToken.Integer) {
                if (reader.Value is int id32) {
                    return new PawnId(id32);
                } else if (reader.Value is long id64) { // BsonReader is doing some dumb shit
                                                        // where it's storing an int as a long, but only tries to parse ints
                    return new PawnId((int)id64);
                }
            }
            return new PawnId(-1);
        }

        public override void WriteJson(JsonWriter writer, PawnId value, JsonSerializer serializer) {
            writer.WriteValue(value.Id);
        }
    }

    public class PawnIdTypeConverter : TypeConverter {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
            if (value is string str) {
                return new PawnId(int.Parse(str));
            }
            throw new NotSupportedException();
        }
    }

    public class IPEndPointConverter : JsonConverter<IPEndPoint> {
        public override IPEndPoint ReadJson(JsonReader reader, Type objectType, IPEndPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value != null) {
                var str = reader.Value.ToString();
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
            return null;
        }

        public override void WriteJson(JsonWriter writer, IPEndPoint value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.Address.ToString()}:{value.Port}");
        }
    }

    public class IPAddressConverter : JsonConverter<IPAddress> {
        public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.String) {
                return IPAddress.Parse(reader.ReadAsString());
            }
            throw new JsonSerializationException($"Could not read IPAddress from {reader.Value.ToString()}");
        }

        public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer) {
            writer.WriteRawValue(value.ToString());
        }
    }

    public class IPEndPointMessageConverter : IMessagePackFormatter<IPEndPoint>
    {
        public IPEndPoint Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            return new IPEndPoint(reader.ReadInt64(), reader.ReadInt32());
        }

        public void Serialize(ref MessagePackWriter writer, IPEndPoint value, MessagePackSerializerOptions options) {
            writer.Write(value.Address.GetAddressBytes());
            writer.WriteInt32(value.Port);
        }
    }
}