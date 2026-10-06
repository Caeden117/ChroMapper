using System.Linq;
using System.Reflection;
using Assets.HSVPicker.UI.TextMeshPro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    // Hue is stored normalized 0..1 in ColorPicker.H and color-distribution offsets, but every picker
    // surface must present degrees 0..360; these tests pin the conversion to the UI boundary so authored
    // values and serialized distribution strings keep their normalized form.
    public class HueDegreesDisplayTest
    {
        private const string PickerPrefabPath = "Assets/_Prefabs/UI/CMUI/Color Picker Component.prefab";

        private static readonly FieldInfo TmpFieldType =
            typeof(ColorTMPField).GetField("type", BindingFlags.NonPublic | BindingFlags.Instance);

        private static ColorTMPField HueField(GameObject root) =>
            root.GetComponentsInChildren<ColorTMPField>(true)
                .Single(f => (ColorValues)TmpFieldType.GetValue(f) == ColorValues.Hue);

        [Test]
        public void PickerHueInputDisplaysNormalizedHueAsDegrees()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath));
            try
            {
                var picker = go.GetComponentInChildren<ColorPicker>();
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();

                picker.H = 0.5f;
                Assert.That(input.text, Is.EqualTo("180"));
                picker.H = 1f;
                Assert.That(input.text, Is.EqualTo("360"));
                picker.H = 0f;
                Assert.That(input.text, Is.EqualTo("0"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PickerHueInputParsesDegreesBackToNormalizedHue()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath));
            try
            {
                var picker = go.GetComponentInChildren<ColorPicker>();
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();

                input.text = "360";
                Assert.That(picker.H, Is.EqualTo(1f).Within(0.0001f));
                input.text = "0";
                Assert.That(picker.H, Is.EqualTo(0f).Within(0.0001f));
                input.text = "180";
                Assert.That(picker.H, Is.EqualTo(0.5f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Degree values grow to three integer digits, so the narrow hue box caps the fraction at two
        // decimals to keep the hundreds digit visible.
        [Test]
        public void PickerHueDisplayRoundsDegreesButTextKeepsPrecision()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath));
            try
            {
                var picker = go.GetComponentInChildren<ColorPicker>();
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();

                picker.H = 123.456f / 360f;
                Assert.That(input.text, Is.EqualTo("123.46"));

                input.text = "44.446";
                Assert.That(picker.H * 360f, Is.EqualTo(44.446f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PickerHueInputClampsDegreeEntryToTheHueRange()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath));
            try
            {
                var picker = go.GetComponentInChildren<ColorPicker>();
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();

                input.text = "720";
                Assert.That(picker.H, Is.EqualTo(1f).Within(0.0001f));
                input.text = "-30";
                Assert.That(picker.H, Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // Picker 2.0 renders hue through a ColorLabel whose serialized display range must span degrees.
        [Test]
        public void PickerTwoHueLabelMapsNormalizedHueToDegrees()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/Picker 2.0.prefab");
            var hueLabels = prefab.GetComponentsInChildren<ColorLabel>(true)
                .Where(l => l.Type == ColorValues.Hue).ToList();

            Assert.That(hueLabels, Is.Not.Empty, "Picker 2.0 must label hue.");
            foreach (var label in hueLabels)
            {
                Assert.That(label.MINValue, Is.EqualTo(0f));
                Assert.That(label.MAXValue, Is.EqualTo(360f));
            }
        }
    }
}
