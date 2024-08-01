using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class BeatmapPropertyTests
    {
        private static readonly BeatmapContractResolver<V2Property> v2Resolver = new();
        private static readonly BeatmapContractResolver<V3Property> v3Resolver = new();
        
        private static readonly JsonSerializerSettings v2Settings = new() { ContractResolver = v2Resolver, };
        private static readonly JsonSerializerSettings v3Settings = new() { ContractResolver = v3Resolver, };


        private const string v2Json = "{\"_field\":\"field test\",\"_property\":\"property test\"}";
        private const string v3Json = "{\"f\":\"field test\",\"p\":\"property test\"}";

        public class DataTest
        {
            [V2Property("_field"), V3Property("f")]
            public string Field;

            [V2Property("_property"), V3Property("p")]
            public string Property { get; set; }
        }

        [Test]
        public void TestV2Deserialization()
        {
            // Data from v2 and v3 JSON should deserialize into the same properties
            var data = JsonConvert.DeserializeObject<DataTest>(v2Json, v2Settings);

            Assert.AreEqual("field test", data.Field);
            Assert.AreEqual("property test", data.Property);
        }

        [Test]
        public void TestV3Deserialization()
        {
            // Data from v2 and v3 JSON should deserialize into the same properties
            var data = JsonConvert.DeserializeObject<DataTest>(v3Json, v3Settings);

            Assert.AreEqual("field test", data.Field);
            Assert.AreEqual("property test", data.Property);
        }

        [Test]
        public void TestV2Serialization()
        {
            // Data from v2 and v3 JSON should serialize into their respective properties
            var data = new DataTest()
            {
                Field = "field test",
                Property = "property test"
            };

            var dataJson = JsonConvert.SerializeObject(data, v2Settings);

            Assert.AreEqual(v2Json, dataJson);
        }

        [Test]
        public void TestV3Serialization()
        {
            // Data from v2 and v3 JSON should serialize into their respective properties
            var data = new DataTest()
            {
                Field = "field test",
                Property = "property test"
            };

            var dataJson = JsonConvert.SerializeObject(data, v3Settings);

            Assert.AreEqual(v3Json, dataJson);
        }
    }
}