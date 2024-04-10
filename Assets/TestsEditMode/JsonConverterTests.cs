using System;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace TestsEditMode
{
    public class JsonConverterTests
    {
        private static readonly Color testColorDefaultAlpha = new Color(0.1f, 0.2f, 0.3f, 1.0f);
        private const string testColorExpectedObjectJson = "{\"r\":0.1,\"g\":0.2,\"b\":0.3}";
        private const string testColorExpectedArrayJson = "[0.1,0.2,0.3]";
        
        private static readonly Color testColorWithAlpha = new Color(0.1f, 0.2f, 0.3f, 0.4f);
        private const string testColorWithAlphaExpectedObjectJson = "{\"r\":0.1,\"g\":0.2,\"b\":0.3,\"a\":0.4}";
        private const string testColorWithAlphaExpectedArrayJson = "[0.1,0.2,0.3,0.4]";
        
        [Test]
        public void TestColorObjectNoAlpha()
        {
            var jsonString = JsonConvert.SerializeObject(testColorDefaultAlpha, Formatting.None, new ColorObjectCoverter());
            Assert.AreEqual(testColorExpectedObjectJson, jsonString);

            var deserializedColor = JsonConvert.DeserializeObject<Color>(jsonString, new ColorObjectCoverter());
            Assert.AreEqual(testColorDefaultAlpha, deserializedColor);
        }
        
        [Test]
        public void TestColorObjectWithAlpha()
        {
            var jsonString = JsonConvert.SerializeObject(testColorWithAlpha, Formatting.None, new ColorObjectCoverter());
            Assert.AreEqual(testColorWithAlphaExpectedObjectJson, jsonString);

            var deserializedColor = JsonConvert.DeserializeObject<Color>(jsonString, new ColorObjectCoverter());
            Assert.AreEqual(testColorWithAlpha, deserializedColor);
        }
        
        [Test]
        public void TestColorArrayNoAlpha()
        {
            var jsonString = JsonConvert.SerializeObject(testColorDefaultAlpha, Formatting.None, new ColorArrayConverter());
            Assert.AreEqual(testColorExpectedArrayJson, jsonString);

            var deserializedColor = JsonConvert.DeserializeObject<Color>(jsonString, new ColorArrayConverter());
            Assert.AreEqual(testColorDefaultAlpha, deserializedColor);
        }
        
        [Test]
        public void TestColorArrayWithAlpha()
        {
            var jsonString = JsonConvert.SerializeObject(testColorWithAlpha, Formatting.None, new ColorArrayConverter());
            Assert.AreEqual(testColorWithAlphaExpectedArrayJson, jsonString);

            var deserializedColor = JsonConvert.DeserializeObject<Color>(jsonString, new ColorArrayConverter());
            Assert.AreEqual(testColorWithAlpha, deserializedColor);
        }

        [Test]
        public void TestColorObjectException()
        {
            var jsonString = JsonConvert.SerializeObject(testColorDefaultAlpha, Formatting.None, new ColorObjectCoverter());

            Assert.Throws<InvalidOperationException>(() => JsonConvert.DeserializeObject<Color>(jsonString, new ColorArrayConverter()));
        }
        
        [Test]
        public void TestColorArrayException()
        {
            var jsonString = JsonConvert.SerializeObject(testColorDefaultAlpha, Formatting.None, new ColorArrayConverter());

            Assert.Throws<InvalidOperationException>(() => JsonConvert.DeserializeObject<Color>(jsonString, new ColorObjectCoverter()));
        }
    }
}