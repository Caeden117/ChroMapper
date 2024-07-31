using Newtonsoft.Json;
using NUnit.Framework;

namespace TestsEditMode
{
    public class BeatmapPropertyTests
    {
        private static readonly BeatmapContractResolver<DataTest> resolver = new();
        private static readonly JsonSerializerSettings settings = new()
        { 
            ContractResolver = resolver
        };


        private const string v2Json = "{\"_field\":\"field test\", \"_property\":\"property test\"}";
        private const string v3Json = "{\"f\":\"field test\", \"p\":\"property test\"}";

        public class DataTest
        {
            [BeatmapProperty("_field")]
            [BeatmapProperty("f")]
            public string Field;

            [BeatmapProperty("_property")]
            [BeatmapProperty("p")]
            public string Property { get; set; }
        }

        [Test]
        public void TestDeserialization()
        {
            // Data from v2 and v3 JSON should deserialize into the same properties
            var dataFromV2 = JsonConvert.DeserializeObject<DataTest>(v2Json, settings);
            var dataFromV3 = JsonConvert.DeserializeObject<DataTest>(v3Json, settings);

            Assert.AreEqual("field test", dataFromV2.Field);
            Assert.AreEqual("field test", dataFromV3.Field);

            Assert.AreEqual("property test", dataFromV2.Property);
            Assert.AreEqual("property test", dataFromV3.Property);
        }
    }
}