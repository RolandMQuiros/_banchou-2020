using System.IO;
using System.Collections.Generic;
using NUnit.Framework;

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

using Banchou.Pawn;
using Banchou.Player;

#pragma warning disable 0618

namespace Banchou.Test {
    public class BsonTests {
        private class IdPair {
            public PlayerId Player;
            public PawnId Pawn;
        }

        [Test]
        public void BsonSerializeDeserializePlayerId() {
            var settings = JsonConvert.DefaultSettings();
            settings.TypeNameHandling = TypeNameHandling.Objects;
            settings.Converters.Add(new Newtonsoft.Json.UnityConverters.Math.Vector3Converter());
            settings.Converters.Add(new Newtonsoft.Json.UnityConverters.Math.Vector2Converter());

            var serializer = JsonSerializer.Create(settings);

            var serStream = new MemoryStream();
            using (var writer = new BsonWriter(serStream)) {
                serializer.Serialize(writer, new IdPair { Player = new PlayerId(12345), Pawn = new PawnId(54321) });
            }

            var desStream = new MemoryStream(serStream.ToArray());
            using (var reader = new BsonReader(desStream)) {
                var pair = serializer.Deserialize<IdPair>(reader);

                Assert.AreEqual(pair.Player.Id, 12345);
                Assert.AreEqual(pair.Pawn.Id, 54321);
            }
        }

        [Test]
        public void PlayerIdDictionaryDeserialize() {
            var dictionary = new Dictionary<PlayerId, string> {
                [new PlayerId(12345)] = "abcde"
            };

            var serialized = JsonConvert.SerializeObject(dictionary);
            var deserialized = JsonConvert.DeserializeObject<Dictionary<PlayerId, string>>(serialized);

            Assert.AreEqual(deserialized[new PlayerId(12345)], "abcde");
        }
    }
}
