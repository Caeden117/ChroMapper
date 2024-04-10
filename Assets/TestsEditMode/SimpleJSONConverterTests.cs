using Newtonsoft.Json;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    public class SimpleJSONConverterTests
    {
        private static readonly JSONObject jsonObject = new()
        {
            ["null"] = null,
            ["0"] = 0,
            ["false"] = false,

            [""] = "",
            ["1"] = 1,
            ["0.0001"] = 0.0001,
            ["true"] = true,
        };

        private static readonly JSONArray jsonArray = new()
        {
            [0] = 0,
            [1] = null,
            [2] = false,
            [3] = 1,
            [4] = "abc",
            [5] = true,
        };
        
        private static readonly JSONNode nestedObject = new JSONArray
        {
            [0] = 123,
            [1] = null,
            [2] = new JSONObject
            {
                ["true"] = true,
                ["false"] = false,
                ["object"] = new JSONObject
                {
                    ["innerTrue"] = true,
                    ["innerFalse"] = false
                }
            }
        };

        [Test]
        public void NewtonsoftSerializeObject()
        {
            var jsonString = JsonConvert.SerializeObject(jsonObject, new SimpleJSONConverter());

            Assert.AreEqual(jsonString, jsonObject.ToString());
        }
        
        [Test]
        public void NewtonsoftSerializeArray()
        {
            var jsonString = JsonConvert.SerializeObject(jsonArray, new SimpleJSONConverter());

            Assert.AreEqual(jsonString, jsonArray.ToString());
        }
        
        [Test]
        public void NewtonsoftSerializeNested()
        {
            var jsonString = JsonConvert.SerializeObject(nestedObject, new SimpleJSONConverter());

            Assert.AreEqual(jsonString, nestedObject.ToString());
        }
        
        [Test]
        public void NewtonsoftDeserializeObject()
        {
            var deserializedNode =
                JsonConvert.DeserializeObject<JSONNode>(jsonObject.ToString(), new SimpleJSONConverter());

            Assert.AreEqual(jsonObject.IsObject, deserializedNode.IsObject);
                
            foreach (var child in jsonObject)
            {
                Assert.True(deserializedNode.HasKey(child.Key));
                Assert.AreEqual(jsonObject[child.Key].ToString(), deserializedNode[child.Key].ToString());
            }
        }
        
        [Test]
        public void NewtonsoftDeserializeArray()
        {
            var deserializedNode =
                JsonConvert.DeserializeObject<JSONNode>(jsonArray.ToString(), new SimpleJSONConverter());

            Assert.AreEqual(jsonArray.Count, deserializedNode.Count);
            Assert.AreEqual(jsonArray.IsArray, deserializedNode.IsArray);
            
            for (var i = 0; i < jsonArray.Count; i++)
            {   
                Assert.AreEqual(jsonArray[i].ToString(), deserializedNode[i].ToString());
            }
        }

        [Test]
        public void NewtonsoftDeserializeNested()
        {
            var deserializedNode =
                JsonConvert.DeserializeObject<JSONNode>(nestedObject.ToString(), new SimpleJSONConverter());

            TestDeserializedRecursive(nestedObject, deserializedNode);
        }

        private void TestDeserializedRecursive(JSONNode expectedNode, JSONNode actualNode)
        {
            Assert.AreEqual(expectedNode.Count, actualNode.Count);
            Assert.AreEqual(expectedNode.IsArray, actualNode.IsArray);
            Assert.AreEqual(expectedNode.IsObject, actualNode.IsObject);
            
            Assert.AreEqual(expectedNode.Value, actualNode.Value);

            if (expectedNode.IsArray)
            {   
                for (var i = 0; i < jsonArray.Count; i++)
                {   
                    TestDeserializedRecursive(expectedNode[i], actualNode[i]);
                }
            }
            else if (expectedNode.IsObject)
            {
                foreach (var child in expectedNode)
                {
                    TestDeserializedRecursive(expectedNode[child.Key], actualNode[child.Key]);
                }
            }
        }
    }
}