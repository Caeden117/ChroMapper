using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Assets.HSVPicker.UI.TextMeshPro;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class ColorPickerPrecisionTest
    {
        private GameObject root;
        private ColorPicker picker;
        private int originalMapVersion;
        private static readonly Color ImportedColor = new Color(0.2468135f, 0.3579246f, 0.4681357f, 0.5792468f);

        [SetUp]
        public void SetUp()
        {
            originalMapVersion = Settings.Instance.MapVersion;
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/UI/CMUI/Color Picker Component.prefab"));
            picker = root.GetComponentInChildren<ColorPicker>();
            picker.CurrentColor = ImportedColor;
        }

        [TearDown]
        public void TearDown()
        {
            Settings.Instance.MapVersion = originalMapVersion;
            Object.DestroyImmediate(root);
        }

        [TestCase(ColorValues.R)]
        [TestCase(ColorValues.G)]
        [TestCase(ColorValues.B)]
        [TestCase(ColorValues.A)]
        public void AssignedChannelsPreservePrecisionBeforeNotifyingListeners(ColorValues channel)
        {
            var notified = ImportedColor;
            picker.ONValueChanged.AddListener(color => notified = color);

            picker.AssignColor(channel, 0.1234567f);

            Assert.AreEqual(0.1234567f, picker.GetValue(channel));
            Assert.AreEqual(0.1234567f, notified[(int)channel]);
            for (var index = 0; index < 4; index++)
            {
                if (index != (int)channel)
                    Assert.AreEqual(ImportedColor[index], picker.CurrentColor[index], "An unedited channel lost imported precision.");
            }
        }

        [TestCase(ColorValues.R)]
        [TestCase(ColorValues.G)]
        [TestCase(ColorValues.B)]
        [TestCase(ColorValues.A)]
        public void SliderEditsRoundAuthoredChannels(ColorValues channel)
        {
            var slider = root.GetComponentsInChildren<ColorSlider>(true).Single(item => item.Type == channel);
            slider.GetComponent<Slider>().value = 0.1234567f;
            Assert.AreEqual(0.123f, picker.GetValue(channel));
        }

        [TestCase(ColorValues.R)]
        [TestCase(ColorValues.G)]
        [TestCase(ColorValues.B)]
        [TestCase(ColorValues.A)]
        public void TextEditsPreserveEnteredChannels(ColorValues channel)
        {
            var typeField = typeof(ColorTMPField).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);
            var field = root.GetComponentsInChildren<ColorTMPField>(true)
                .Single(item => (ColorValues)typeField.GetValue(item) == channel);
            field.GetComponent<TMPro.TMP_InputField>().text = "0.1234567";
            Assert.AreEqual(0.1234567f, picker.GetValue(channel));
        }

        // HSV edits author three new RGB channels, but must not change the imported alpha.
        [TestCase(ColorValues.Hue)]
        [TestCase(ColorValues.Saturation)]
        [TestCase(ColorValues.Value)]
        public void HsvEditsRoundGeneratedRgb(ColorValues channel)
        {
            var slider = root.GetComponentsInChildren<ColorSlider>(true).Single(item => item.Type == channel);
            slider.GetComponent<Slider>().value = 0.1234567f;
            var converted = HSVUtil.ConvertHsvToRgb(picker.H * 360, picker.S, picker.V, ImportedColor.a);
            var expected = new Color(
                (float)Math.Round(converted.r, 3),
                (float)Math.Round(converted.g, 3),
                (float)Math.Round(converted.b, 3),
                ImportedColor.a);
            Assert.AreEqual(expected, picker.CurrentColor);
        }

        [TestCase(ColorValues.Hue, "123.456789")]
        [TestCase(ColorValues.Saturation, "0.123456789")]
        [TestCase(ColorValues.Value, "0.123456789")]
        public void HsvTextEditsSaveRoundedRgb(ColorValues channel, string text)
        {
            GetInput(channel).text = text;

            var converted = HSVUtil.ConvertHsvToRgb(picker.H * 360, picker.S, picker.V, ImportedColor.a);
            var json = new JSONArray().WriteColor(picker.CurrentColor);
            var saved = JSON.Parse(json.ToString());
            for (var index = 0; index < 3; index++)
            {
                var expected = (float)Math.Round(converted[index], 3);
                Assert.AreEqual(expected, picker.CurrentColor[index]);
                Assert.AreEqual(expected.ToString("0.###", CultureInfo.InvariantCulture), json[index].Value);
                Assert.AreEqual(expected, saved[index].AsFloat);
            }

            Assert.AreEqual(ImportedColor.a, saved[3].AsFloat);
        }

        [TestCase(ColorValues.Hue, 0.3429366f, "123.457")]
        [TestCase(ColorValues.Saturation, 0.1234567f, "0.123")]
        [TestCase(ColorValues.Value, 0.7654321f, "0.765")]
        public void HsvSliderDisplaysThreeDecimalPlaces(ColorValues channel, float value, string expected)
        {
            var slider = root.GetComponentsInChildren<ColorSlider>(true).Single(item => item.Type == channel);
            slider.GetComponent<Slider>().value = value;

            Assert.AreEqual(expected, GetInput(channel).text);
            Assert.AreEqual(value, picker.GetValue(channel), "Display rounding must not quantize HSV coordinates.");
        }

        [Test]
        public void ColorSquareSavesRoundedRgbAfterRepeatedMoves()
        {
            var slider = root.GetComponentInChildren<BoxSlider>(true);
            var values = new[] { 0.1234567f, 0.7654321f, 0.3456789f };
            foreach (var value in values)
            {
                slider.NormalizedValue = value;
                slider.NormalizedValueY = 1f - value;
                var json = new JSONArray().WriteColor(picker.CurrentColor);
                var saved = JSON.Parse(json.ToString());
                for (var index = 0; index < 3; index++)
                {
                    var expected = (float)Math.Round(picker.CurrentColor[index], 3);
                    Assert.AreEqual(expected, picker.CurrentColor[index]);
                    Assert.AreEqual(expected.ToString("0.###", CultureInfo.InvariantCulture), json[index].Value);
                    Assert.AreEqual(expected, saved[index].AsFloat);
                }
            }
        }

        private TMPro.TMP_InputField GetInput(ColorValues channel)
        {
            var typeField = typeof(ColorTMPField).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);
            return root.GetComponentsInChildren<ColorTMPField>(true)
                .Single(item => (ColorValues)typeField.GetValue(item) == channel)
                .GetComponent<TMPro.TMP_InputField>();
        }

        [Test]
        public void LoadingAndDisplayingColorPreservesImportedPrecision()
        {
            Assert.AreEqual(ImportedColor, picker.CurrentColor);
            Assert.AreEqual(ImportedColor, JSON.Parse(new JSONArray().WriteColor(picker.CurrentColor).ToString()).ReadColor());
        }

        [Test]
        public void LoadingNearbyColorPreservesItsDistinctPrecision()
        {
            var nearby = ImportedColor;
            nearby.r += 0.000001f;
            picker.CurrentColor = nearby;
            Assert.AreEqual(nearby, picker.CurrentColor);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SliderColorSavesRoundedFloatChannels(bool asObject)
        {
            var sliders = root.GetComponentsInChildren<ColorSlider>(true);
            var entries = new[] { 0.1234567f, 0.2345678f, 0.3456789f, 0.4567891f };
            for (var index = 0; index < 4; index++)
            {
                var slider = sliders.Single(item => (int)item.Type == index);
                slider.GetComponent<Slider>().value = entries[index];
            }

            JSONNode json = asObject ? new JSONObject() : new JSONArray();
            var saved = JSON.Parse(json.WriteColor(picker.CurrentColor).Clone().ToString());
            var keys = new[] { "r", "g", "b", "a" };
            var expected = new[] { 0.123f, 0.235f, 0.346f, 0.457f };
            for (var index = 0; index < 4; index++)
                Assert.AreEqual(expected[index], (asObject ? saved[keys[index]] : saved[index]).AsFloat);
        }

        [Test]
        public void HexEntryRoundsRgbAndPreservesParsedAlpha()
        {
            var field = root.GetComponentInChildren<HexColorField>(true);
            Assert.IsNotNull(field, "The picker prefab must expose the hex-entry path.");
            field.GetComponent<InputField>().onEndEdit.Invoke("#12345678");
            ColorUtility.TryParseHtmlString("#12345678", out var expected);
            expected.r = (float)Math.Round(expected.r, 3);
            expected.g = (float)Math.Round(expected.g, 3);
            expected.b = (float)Math.Round(expected.b, 3);
            var saved = JSON.Parse(new JSONArray().WriteColor(picker.CurrentColor).ToString());
            for (var index = 0; index < 4; index++)
            {
                Assert.AreEqual(expected[index], picker.CurrentColor[index]);
                Assert.AreEqual(expected[index], saved[index].AsFloat);
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void TextAlphaSurvivesEventSave(int version)
        {
            var typeField = typeof(ColorTMPField).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);
            var field = root.GetComponentsInChildren<ColorTMPField>(true)
                .Single(item => (ColorValues)typeField.GetValue(item) == ColorValues.A);
            field.GetComponent<TMPro.TMP_InputField>().text = "0.000001";
            Assert.AreEqual(0.000001f, picker.CurrentColor.a);

            Settings.Instance.MapVersion = version;
            var evt = new BaseEvent { Type = 1, CustomColor = picker.CurrentColor };
            var saved = JSON.Parse(evt.ToJson().ToString());
            var customKey = version == 2 ? "_customData" : "customData";
            var colorKey = version == 2 ? "_color" : "color";
            Assert.AreEqual(0.000001f, saved[customKey][colorKey][3].AsFloat);
        }

        [Test]
        public void DisplayingSmallImportedAlphaDoesNotShowZero()
        {
            var color = ImportedColor;
            color.a = 0.000001f;
            picker.CurrentColor = color;
            var typeField = typeof(ColorTMPField).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);
            var field = root.GetComponentsInChildren<ColorTMPField>(true)
                .Single(item => (ColorValues)typeField.GetValue(item) == ColorValues.A);
            Assert.AreEqual(color.a, float.Parse(field.GetComponent<TMPro.TMP_InputField>().text));
        }
    }
}
