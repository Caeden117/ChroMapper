using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.V2;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    // Saving at three beat decimals must preserve fog, transforms, colors and animation values.
    public class BeatOnlyJsonPrecisionTest
    {
        private int originalMapVersion;
        private int originalPrecision;
        private bool originalCap;

        // Exercise the reported setting without inheriting precision from another editor test.
        [SetUp]
        public void SetUp()
        {
            originalMapVersion = Settings.Instance.MapVersion;
            originalPrecision = JSONNumber.DecimalPrecision;
            originalCap = JSONNumber.CapNumbersToDecimals;
            JSONNumber.DecimalPrecision = 3;
            JSONNumber.CapNumbersToDecimals = true;
        }

        // JSON precision and map version are shared editor state and must survive failed assertions.
        [TearDown]
        public void TearDown()
        {
            Settings.Instance.MapVersion = originalMapVersion;
            JSONNumber.DecimalPrecision = originalPrecision;
            JSONNumber.CapNumbersToDecimals = originalCap;
        }

        // The reported Billie fog attenuation becomes zero only when the output is serialized.
        [TestCase(3)]
        [TestCase(4)]
        public void EnvironmentComponentsSurviveSave(int version)
        {
            // V4 enhancement nodes use the V3 schema; its difficulty exporter does not yet emit enhancements.
            Settings.Instance.MapVersion = 3;
            var map = new BaseDifficulty();
            map.EnvironmentEnhancements.Add(new BaseEnvironmentEnhancement(JSON.Parse(@"{
                ""id"": ""BillieEnvironment.[0]Environment"", ""lookupMethod"": ""Exact"",
                ""position"": [0.00001, 1.234567, -2.345678],
                ""scale"": [0.00001, 1.234567, 2.345678],
                ""rotation"": [1.234567, 2.345678, 3.456789],
                ""components"": { ""BloomFogEnvironment"": {
                    ""attenuation"": 0.00001, ""startY"": -9999, ""height"": 1,
                    ""offset"": 0.00000000123456789
                } }
            }")));
            Settings.Instance.MapVersion = version;
            var output = version == 3
                ? V3Difficulty.GetOutputJson(map)
                : map.EnvironmentEnhancements[0].ToJson();
            var serialized = JSON.Parse(output.ToString());
            var saved = version == 3 ? serialized["customData"]["environment"][0] : serialized;
            Assert.True(saved["components"].HasKey("BloomFogEnvironment"));
            var fog = saved["components"]["BloomFogEnvironment"];
            Assert.AreEqual(0.00001, fog["attenuation"].AsDouble, "Fog attenuation was rounded during save.");
            Assert.AreEqual(0.00000000123456789, fog["offset"].AsDouble);
            Assert.AreEqual(-9999, fog["startY"].AsInt);
            Assert.AreEqual(1, fog["height"].AsInt);
            Assert.AreEqual(0.00001f, saved["position"][0].AsFloat);
            Assert.AreEqual(1.234567f, saved["scale"][1].AsFloat);
            Assert.AreEqual(3.456789f, saved["rotation"][2].AsFloat);
        }

        // Doubles carrying exact float data emit the shortest float form instead of the
        // double-conversion tail the round-trip formatter used to expose.
        [TestCase(-9.3059749603271484, "-9.305975")]
        [TestCase(23.277999877929688, "23.278")]
        [TestCase(6.9380006790161133, "6.93800068")]
        [TestCase(0.10000000149011612, "0.1")]
        [TestCase(585.0, "585")]
        public void FloatBackedDoublesSerializeAsShortestFloat(double value, string expected)
        {
            var node = new JSONObject { ["v"] = value };
            Assert.AreEqual(expected, node["v"].Value);
            Assert.AreEqual(
                (float)value,
                JSON.Parse(node.ToString())["v"].AsFloat,
                "Serialized value must round-trip to the same float");
        }

        // Unknown custom-data numbers and normalized animation times have no beat semantics.
        [TestCase(0.00001)]
        [TestCase(0.00000000123456789)]
        [TestCase(0.12345678901234567)]
        [TestCase(-9999.123456789)]
        public void ArbitraryJsonNumbersRoundTrip(double value)
        {
            var data = new JSONObject { ["value"] = value };
            Assert.AreEqual(value, JSON.Parse(data.Clone().ToString())["value"].AsDouble);
        }

        // WriteColor previously discarded precision before JSONNumber even serialized the data.
        [TestCase(false)]
        [TestCase(true)]
        public void AuthoredColorChannelsSurviveWriteColor(bool asObject)
        {
            var color = new Color(0.00001f, 0.1234567f, 1.234567f, 0.7654321f);
            JSONNode data = asObject ? new JSONObject() : new JSONArray();
            var saved = JSON.Parse(data.WriteColor(color).ToString()).ReadColor();
            Assert.AreEqual(color, saved);
        }

        // All four GLS node types carry non-beat values beside their relative beat times.
        [Test]
        public void GlsValuesSurviveV3AndV4Serialization()
        {
            var value = 0.00001f;
            Assert.AreEqual(value, JSON.Parse(V3LightColorBase.ToJson(new BaseLightColorBase { Brightness = value }).ToString())["s"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(V3LightRotationBase.ToJson(new BaseLightRotationBase { Rotation = value }).ToString())["r"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(V3LightTranslationBase.ToJson(new BaseLightTranslationBase { Translation = value }).ToString())["t"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(V3FloatFxEvent.ToJson(new BaseFxEventFloat { Value = value }).ToString())["v"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(new V4CommonData.LightColorEvent { Brightness = value }.ToJson().ToString())["b"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(new V4CommonData.LightRotationEvent { Rotation = value }.ToJson().ToString())["r"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(new V4CommonData.LightTranslationEvent { Translation = value }.ToJson().ToString())["t"].AsFloat);
            Assert.AreEqual(value, JSON.Parse(new V4CommonData.FloatFxEvent { Value = value }.ToJson().ToString())["v"].AsFloat);
        }

        // Mapping-session time is measured in minutes, so beat precision must not shorten it.
        [TestCase(2)]
        [TestCase(3)]
        public void MappingMinutesSurviveDifficultySave(int version)
        {
            Settings.Instance.MapVersion = version;
            var map = new BaseDifficulty { Time = 1.234567f };
            var output = version == 2 ? V2Difficulty.GetOutputJson(map) : V3Difficulty.GetOutputJson(map);
            var prefix = version == 2 ? "_" : "";
            Assert.AreEqual(map.Time, JSON.Parse(output.ToString())[prefix + "customData"][prefix + "time"].AsFloat);
        }

        // Custom-event positions and normalized point times must survive alongside rounded event beats.
        [TestCase(2)]
        [TestCase(3)]
        public void CustomEventAndPointValuesSurviveDifficultySave(int version)
        {
            Settings.Instance.MapVersion = version;
            var prefix = version == 2 ? "_" : "";
            var map = new BaseDifficulty();
            map.PointDefinitions.Add("tiny", JSON.Parse("[[0.00001, 0.123456789, 0, 0.123456789]]").AsArray);
            var data = new JSONObject
            {
                [prefix + "track"] = "fog",
                // Duration is a beat count; the point's fourth coordinate is normalized animation progress.
                [prefix + "duration"] = 1.234567,
                [prefix + "position"] = JSON.Parse("[[0.00001, 0.123456789, 0, 0.123456789]]")
            };
            map.CustomEvents.Add(new BaseCustomEvent(new JSONObject
            {
                [version == 2 ? "_time" : "b"] = 1.234567,
                [version == 2 ? "_type" : "t"] = "AnimateTrack",
                [version == 2 ? "_data" : "d"] = data
            }));
            var output = version == 2 ? V2Difficulty.GetOutputJson(map) : V3Difficulty.GetOutputJson(map);
            var saved = JSON.Parse(output.ToString())[prefix + "customData"];
            var evt = saved[prefix + "customEvents"][0];
            var eventData = evt[version == 2 ? "_data" : "d"];
            // Rounding the export duration must leave the live event's authored data available for editing.
            Assert.AreEqual(1.235, eventData[prefix + "duration"].AsDouble);
            Assert.AreEqual(1.234567, map.CustomEvents[0].Data[prefix + "duration"].AsDouble);
            Assert.AreEqual(0.00001, eventData[prefix + "position"][0][0].AsDouble);
            Assert.AreEqual(0.123456789, eventData[prefix + "position"][0][3].AsDouble);
            var points = version == 2
                ? saved["_pointDefinitions"][0]["_points"]
                : saved["pointDefinitions"]["tiny"];
            Assert.AreEqual(0.123456789, points[0][1].AsDouble);
            Assert.AreEqual(1.235, evt[version == 2 ? "_time" : "b"].AsDouble);
        }

        // Ordinary note beats still obey the selected beat precision in every file version.
        [Test]
        public void NoteBeatsStillRoundToThreeDecimals()
        {
            var note = new BaseNote { JsonTime = 1.234567f };
            Assert.AreEqual(1.235, JSON.Parse(V2Note.ToJson(note).ToString())["_time"].AsDouble);
            Assert.AreEqual(1.235, JSON.Parse(V3ColorNote.ToJson(note).ToString())["b"].AsDouble);
            Assert.AreEqual(1.235, JSON.Parse(V4ColorNote.ToJson(note, new System.Collections.Generic.List<V4CommonData.Note> { V4CommonData.Note.FromBaseNote(note) }).ToString())["b"].AsDouble);
        }
    }
}
