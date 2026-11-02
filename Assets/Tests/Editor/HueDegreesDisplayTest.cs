using System.Collections;
using System.Linq;
using System.Reflection;
using Assets.HSVPicker.UI.TextMeshPro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

        [Test]
        public void PickerHueDisplayRoundsDegreesButTextKeepsPrecision()
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath));
            try
            {
                var picker = go.GetComponentInChildren<ColorPicker>();
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();

                picker.H = 123.456f / 360f;
                Assert.That(input.text, Is.EqualTo("123.456"));

                input.text = "44.446";
                Assert.That(picker.H * 360f, Is.EqualTo(44.446f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator PickerHueDisplayFitsThreeDecimals()
        {
            var canvasObject = new GameObject("Picker canvas", typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PickerPrefabPath), canvasObject.transform);
            try
            {
                // CMUI normally gives this prefab its containing dialog's width.
                go.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300f);
                var picker = go.GetComponentInChildren<ColorPicker>();
                if (!picker.Setup.ShowHsv)
                    picker.ToggleColorSliders();

                yield return null;

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(go.GetComponent<RectTransform>());
                picker.H = 123.456f / 360f;
                var input = HueField(go).GetComponent<TMPro.TMP_InputField>();
                Assert.That(input.text, Is.EqualTo("123.456"));
                Assert.That(input.textViewport.rect.width, Is.GreaterThan(0f));
                Assert.That(input.textComponent.GetPreferredValues(input.text).x, Is.LessThanOrEqualTo(input.textViewport.rect.width));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
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

        private const string RowPrefabPath = "Assets/_Prefabs/UI/GLS Color Distribution Row.prefab";

        // The row prefab is authored inactive and activated by the array view controller when added,
        // so tests must activate the clone for Awake to wire the component callbacks.
        private static GLSColorDistributionRowView SpawnRow()
        {
            var go = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath));
            go.SetActive(true);
            return go.GetComponent<GLSColorDistributionRowView>();
        }

        [Test]
        public void DistributionRowDisplaysHueOffsetAsDegrees()
        {
            var row = SpawnRow();
            try
            {
                row.SetDistribution("h,0.5,IO^2");
                Assert.That(row.ValueInput.Value, Is.EqualTo(180f).Within(0.001f));
                row.SetDistribution("h,1,L,l");
                Assert.That(row.ValueInput.Value, Is.EqualTo(360f).Within(0.001f));
                row.SetDistribution("h,0,L");
                Assert.That(row.ValueInput.Value, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }

        [Test]
        public void DistributionRowParsesDegreesBackToNormalizedOffset()
        {
            var row = SpawnRow();
            try
            {
                row.SetDistribution("h,0,IO^2");

                row.ValueInput.Value = 360f;
                Assert.That(row.GetDistribution(), Is.EqualTo("h,1,IO^2"));
                row.ValueInput.Value = 180f;
                Assert.That(row.GetDistribution(), Is.EqualTo("h,0.5,IO^2"));
                row.ValueInput.Value = 0f;
                Assert.That(row.GetDistribution(), Is.EqualTo("h,0,IO^2"));
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }

        [Test]
        public void DistributionRowLeavesNonHueOffsetsUnscaled()
        {
            var row = SpawnRow();
            try
            {
                row.SetDistribution("v,0.7,L");
                Assert.That(row.ValueInput.Value, Is.EqualTo(0.7f).Within(0.001f));
                row.ValueInput.Value = 2f;
                Assert.That(row.GetDistribution(), Is.EqualTo("v,2,L"));
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }

        // The stored offset stays authored-normalized, so switching targets re-renders the same
        // offset in the new target's units instead of leaking a degrees value into another channel.
        [Test]
        public void DistributionRowRescalesDisplayedValueWhenTargetChanges()
        {
            var row = SpawnRow();
            try
            {
                row.SetDistribution("h,0.5,IO^2");
                Assert.That(row.ValueInput.Value, Is.EqualTo(180f).Within(0.001f));

                row.TargetDropdown.Value = 2;
                Assert.That(row.ValueInput.Value, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(row.GetDistribution(), Is.EqualTo("v,0.5,IO^2"));

                row.TargetDropdown.Value = 0;
                Assert.That(row.ValueInput.Value, Is.EqualTo(180f).Within(0.001f));
                Assert.That(row.GetDistribution(), Is.EqualTo("h,0.5,IO^2"));
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }
    }
}
