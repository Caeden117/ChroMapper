using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class SimpleJSONHelperTest
    {
        [Test]
        public void RemovesFromJsonObject()
        {
            var json = new JSONObject
            {
                ["null"] = null,
                ["0"] = 0,
                ["false"] = false,

                [""] = "",
                ["1"] = 1,
                ["0.0001"] = 0.0001,
                ["true"] = true,
            };

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);

            Assert.False(json.HasKey("null"));
            Assert.False(json.HasKey("0"));
            Assert.False(json.HasKey("false"));

            Assert.True(json.HasKey(""));
            Assert.True(json.HasKey("1"));
            Assert.True(json.HasKey("0.0001"));
            Assert.True(json.HasKey("true"));
        }

        [Test]
        public void DoesNotRemoveFromJsonArray()
        {
            var json = new JSONArray
            {
                [0] = 0,
                [1] = null,
                [2] = false,
                [3] = 1,
                [4] = "abc",
                [5] = true,
            };

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);
            Assert.AreEqual(6, json.Count);
        }

        [Test]
        public void RemovesFromNestedJsonObjects()
        {
            var json = new JSONArray
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

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);

            Assert.AreEqual(3, json.Count);

            var child = json[2];
            Assert.False(child.HasKey("false"));
            Assert.True(child.HasKey("true"));
            Assert.True(child.HasKey("object"));

            var innerChild = child["object"];
            Assert.False(innerChild.HasKey("innerFalse"));
            Assert.True(innerChild.HasKey("innerTrue"));
        }

        [Test]
        public void TestItWithANote()
        {
            var note = new V3ColorNote
            {
                JsonTime = 8,
                CutDirection = 8
            };
            var json = note.ToJson();

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);

            Assert.True(json.HasKey("b"));
            Assert.True(json.HasKey("d"));

            Assert.False(json.HasKey("a"));
            Assert.False(json.HasKey("x"));
            Assert.False(json.HasKey("y"));
            Assert.False(json.HasKey("c"));
        }

        [Test]
        public void DoesNotRemoveV3CustomData()
        {
            var note = new V3ColorNote
            {
                CustomColor = new UnityEngine.Color(0, 1, 0)
            };
            note.WriteCustom();
            var json = note.ToJson();

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);

            Assert.True(json.HasKey("customData"));
        }

        [Test]
        public void DoesNotRemoveV2CustomData()
        {
            var note = new V2Note
            {
                CustomColor = new UnityEngine.Color(0, 1, 0)
            };
            note.WriteCustom();
            var json = note.ToJson();

            SimpleJSONHelper.RemovePropertiesWithDefaultValues(json);

            Assert.True(json.HasKey("_customData"));
        }
    }

}