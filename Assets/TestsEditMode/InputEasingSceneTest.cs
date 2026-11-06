using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestsEditMode
{
    // Load the mapper in a preview scene to inspect its serialized controls without running gameplay Start
    // methods.
    public class InputEasingSceneTest
    {
        private Scene scene;
        private SerializedObject view;
        private RectTransform tab;

        [OneTimeSetUp]
        public void OpenMapperPreview()
        {
            // Initialize Settings before scene deserialization. CustomPlatformsLoader otherwise first
            // accesses it from a MonoBehaviour field initializer, where MultiSettings' Random.ColorHSV call
            // is forbidden by Unity.
            _ = CustomPlatformSettings.Instance;

            scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/03_Mapper.unity");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<InputEasingViewController>(true))
                {
                    view = new SerializedObject(candidate);
                    break;
                }
                if (view != null)
                    break;
            }
            Assert.That(view, Is.Not.Null, "Mapper must contain the easing view controller.");
            var targets = view.FindProperty("showTargets");
            var target = (GameObject)targets.GetArrayElementAtIndex(0).objectReferenceValue;
            tab = (RectTransform)target.transform;
        }

        [OneTimeTearDown]
        public void CloseMapperPreview()
        {
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
        }

        [TestCase("easeSineToggle", "Sn")]
        [TestCase("easeCubicToggle", "^3")]
        [TestCase("easeQuarticToggle", "^4")]
        [TestCase("easeQuinticToggle", "^5")]
        [TestCase("easeExponentialToggle", "Ex")]
        public void ExtendedFamilyIsWiredBelowNativeControls(string field, string abbreviation)
        {
            var component = GetToggle(field);
            var serialized = new SerializedObject(component);
            var selectable = (Component)serialized.FindProperty("<Selectable>k__BackingField").objectReferenceValue;
            Assert.That(selectable, Is.Not.Null, field + " needs a real selectable.");
            Assert.That(serialized.FindProperty("toggle").objectReferenceValue, Is.EqualTo(selectable));
            Assert.That(selectable.transform.IsChildOf(component.transform), Is.True);
            Assert.That(component.gameObject.activeSelf, Is.True);
            Assert.That(component.transform.IsChildOf(tab), Is.True, "Tab visibility must own every family.");
            AssertLabel(component, abbreviation);

            // The controller manages family exclusivity. Match the native controls' null ToggleGroup.
            var native = GetToggle("easeElasticToggle");
            var nativeSelectable = new SerializedObject(native).FindProperty("toggle").objectReferenceValue;
            var nativeGroup = new SerializedObject(nativeSelectable).FindProperty("m_Group").objectReferenceValue;
            Assert.That(new SerializedObject(selectable).FindProperty("m_Group").objectReferenceValue, Is.EqualTo(nativeGroup));

            RebuildTabLayout();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(tab, component.transform);
            foreach (var nativeField in new[] { "easeNoneToggle", "easeLinearToggle", "easeQuadToggle", "easeCircularToggle", "easeBounceToggle", "easeBackToggle", "easeElasticToggle" })
            {
                var nativeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(tab, GetToggle(nativeField).transform);
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(nativeBounds.min.y + 0.01f), field + " must be visibly below " + nativeField);
            }
            Assert.That(bounds.size.x, Is.GreaterThanOrEqualTo(15.9f));
            Assert.That(bounds.size.y, Is.GreaterThanOrEqualTo(15.9f));
        }

        [TestCase("easeQuadToggle", "^2")]
        [TestCase("easeBackToggle", "Bk")]
        [TestCase("easeCircularToggle", "Cr")]
        public void NativeFamilyUsesUpdatedAbbreviation(string field, string abbreviation)
        {
            AssertLabel(GetToggle(field), abbreviation);
        }

        [TestCase("Sine")]
        [TestCase("Cubic")]
        [TestCase("Quartic")]
        [TestCase("Quintic")]
        [TestCase("Exponential")]
        public void ExtendedFamilyHasLocalizedRequirementTooltip(string family)
        {
            var key = "place.gls.easings." + family.ToLowerInvariant() + ".tooltip";
            var shared = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Locales/Mapper Shared Data.asset"));
            var entry = FindEntry(shared.FindProperty("m_Entries"), "m_Key", key);
            Assert.That(entry, Is.Not.Null, "Missing Mapper shared tooltip key: " + key);
            var id = entry.FindPropertyRelative("m_Id").longValue;
            var english = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Locales/Mapper_en.asset"));
            var translation = FindEntry(english.FindProperty("m_TableData"), "m_Id", id.ToString());
            Assert.That(translation, Is.Not.Null, "Missing English tooltip for " + family);
            Assert.That(translation.FindPropertyRelative("m_Localized").stringValue, Is.EqualTo(family + "\nUsing this will cause a suggestion for ChromaGLS to be added if used."));

            var component = GetToggle("ease" + family + "Toggle");
            var tooltip = component.GetComponentInChildren<Tooltip>(true);
            Assert.That(tooltip, Is.Not.Null, "The family prefab needs a localized hover tooltip.");
            Assert.That(tooltip.enabled, Is.True);
            var serialized = new SerializedObject(tooltip);
            Assert.That(serialized.FindProperty("TooltipOverride").stringValue, Is.Null.Or.Empty, "Do not bypass localization.");
            var localized = serialized.FindProperty("LocalizedTooltip");
            var tableName = localized.FindPropertyRelative("m_TableReference.m_TableCollectionName").stringValue;
            Assert.That(tableName, Is.EqualTo("Mapper").Or.EqualTo("GUID:8944026aec77cf8479a89f2280c8e1ff"));
            var reference = localized.FindPropertyRelative("m_TableEntryReference");
            Assert.That(reference.FindPropertyRelative("m_KeyId").longValue == id || reference.FindPropertyRelative("m_Key").stringValue == key, Is.True);
            Assert.That(localized.FindPropertyRelative("m_FallbackState").intValue, Is.EqualTo(2), "UseFallback keeps untranslated locales usable.");
        }

        // A locale with no fallback metadata cannot resolve English through UseFallback. Check every
        // supported locale for usable text.
        [TestCase("Sine")]
        [TestCase("Cubic")]
        [TestCase("Quartic")]
        [TestCase("Quintic")]
        [TestCase("Exponential")]
        public void ExtendedFamilyTooltipResolvesForEveryLocale(string family)
        {
            var key = "place.gls.easings." + family.ToLowerInvariant() + ".tooltip";
            var shared = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Locales/Mapper Shared Data.asset"));
            var entry = FindEntry(shared.FindProperty("m_Entries"), "m_Key", key);
            Assert.That(entry, Is.Not.Null, "Missing shared tooltip key: " + key);
            var id = entry.FindPropertyRelative("m_Id").longValue;
            var locales = AssetDatabase.FindAssets("t:Locale", new[] { "Assets/Locales" });
            Assert.That(locales, Is.Not.Empty);
            foreach (var guid in locales)
            {
                var locale = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var visited = new System.Collections.Generic.HashSet<UnityEngine.Object>();
                Assert.That(HasLocalizedEntryOrFallback(locale, id, visited), Is.True,
                    locale.name + " has neither a translated " + family + " warning nor a usable fallback.");
            }
        }

        [Test]
        public void VisualizeGlsLightTransitionsOptionIsWiredAndLocalized()
        {
            var optionsScene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            try
            {
                SimpleSettingsBinder binder = null;
                foreach (var root in optionsScene.GetRootGameObjects())
                {
                    foreach (var candidate in root.GetComponentsInChildren<SimpleSettingsBinder>(true))
                    {
                        if (candidate.BindedSetting == "VisualizeGLSLightTransitions")
                        {
                            binder = candidate;
                            break;
                        }
                    }

                    if (binder != null)
                    {
                        break;
                    }
                }

                Assert.That(binder, Is.Not.Null, "Graphics must contain the GLS transition-ribbon toggle.");
                var betterToggle = binder.GetComponent<BetterToggle>();
                Assert.That(betterToggle.IsOn, Is.True);
                var toggle = new SerializedObject(betterToggle);
                var description = toggle.FindProperty("Description").objectReferenceValue;
                Assert.That(new SerializedObject(description).FindProperty("m_text").stringValue,
                    Is.EqualTo("Visualize GLS Light Transitions"));
                Assert.That(HasAncestorNamed(binder.transform, "Graphics Panel"), Is.True);

                var shared = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Locales/Options Shared Data.asset"));
                var title = FindEntry(
                    shared.FindProperty("m_Entries"),
                    "m_Key",
                    "options.graphics.visualize-gls-light-transitions");
                var tooltip = FindEntry(
                    shared.FindProperty("m_Entries"),
                    "m_Key",
                    "options.graphics.visualize-gls-light-transitions.tooltip");
                Assert.That(title, Is.Not.Null);
                Assert.That(tooltip, Is.Not.Null);
                var titleId = title.FindPropertyRelative("m_Id").longValue;
                var tooltipId = tooltip.FindPropertyRelative("m_Id").longValue;

                var titleLocalizerId = 0L;
                var titleTable = string.Empty;
                foreach (var component in binder.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var property = new SerializedObject(component).FindProperty("m_StringReference.m_TableEntryReference.m_KeyId");
                    if (property != null)
                    {
                        titleLocalizerId = property.longValue;
                        titleTable = new SerializedObject(component)
                            .FindProperty("m_StringReference.m_TableReference.m_TableCollectionName").stringValue;
                        break;
                    }
                }

                Assert.That(titleLocalizerId, Is.EqualTo(titleId));
                Assert.That(titleTable, Is.EqualTo("Options").Or.EqualTo("GUID:3d3fb288927a2264891ce15662e46756"));
                var tooltipComponent = binder.GetComponent<Tooltip>();
                Assert.That(tooltipComponent, Is.Not.Null);
                var serializedTooltip = new SerializedObject(tooltipComponent);
                var tooltipReference = serializedTooltip.FindProperty("LocalizedTooltip.m_TableEntryReference.m_KeyId");
                Assert.That(tooltipReference.longValue, Is.EqualTo(tooltipId));
                var tooltipTable = serializedTooltip.FindProperty("LocalizedTooltip.m_TableReference.m_TableCollectionName");
                Assert.That(tooltipTable.stringValue,
                    Is.EqualTo("Options").Or.EqualTo("GUID:3d3fb288927a2264891ce15662e46756"));

                var optionTables = AssetDatabase.FindAssets("Options_", new[] { "Assets/Locales" });
                var localizedTableCount = 0;
                foreach (var guid in optionTables)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".asset", StringComparison.Ordinal)
                        || !System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("Options_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    localizedTableCount++;
                    var table = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
                    var localizedTitle = FindEntry(table.FindProperty("m_TableData"), "m_Id", titleId.ToString());
                    var localizedTooltip = FindEntry(table.FindProperty("m_TableData"), "m_Id", tooltipId.ToString());
                    Assert.That(localizedTitle, Is.Not.Null, path);
                    Assert.That(localizedTooltip, Is.Not.Null, path);
                    Assert.That(localizedTitle.FindPropertyRelative("m_Localized").stringValue,
                        Is.EqualTo("Visualize GLS Light Transitions"), path);
                    Assert.That(localizedTooltip.FindPropertyRelative("m_Localized").stringValue,
                        Is.EqualTo("Shows GLS light color event tween ribbons on the editor grid."), path);
                }

                Assert.That(localizedTableCount, Is.EqualTo(15));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(optionsScene);
            }
        }

        // Traverse the imported hierarchy instead of trusting serialized parent fileIDs when checking the option category.
        private static bool HasAncestorNamed(Transform child, string name)
        {
            for (var current = child; current != null; current = current.parent)
            {
                if (current.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        // Consult the locale's real fallback enumeration without introducing localization package references into the edit-mode assembly.
        private static bool HasLocalizedEntryOrFallback(UnityEngine.Object locale, long id,
            System.Collections.Generic.HashSet<UnityEngine.Object> visited)
        {
            if (!visited.Add(locale))
                return false;
            var code = new SerializedObject(locale).FindProperty("m_Identifier.m_Code").stringValue;
            var table = AssetDatabase.LoadMainAssetAtPath("Assets/Locales/Mapper_" + code + ".asset");
            if (table != null)
            {
                var entry = FindEntry(new SerializedObject(table).FindProperty("m_TableData"), "m_Id", id.ToString());
                if (entry != null && !string.IsNullOrEmpty(entry.FindPropertyRelative("m_Localized").stringValue))
                    return true;
            }
            var fallbacks = (System.Collections.IEnumerable)locale.GetType().GetMethod("GetFallbacks").Invoke(locale, null);
            foreach (UnityEngine.Object fallback in fallbacks)
            {
                if (HasLocalizedEntryOrFallback(fallback, id, visited))
                    return true;
            }
            return false;
        }

        private ToggleComponent GetToggle(string field)
        {
            var property = view.FindProperty(field);
            Assert.That(property, Is.Not.Null, "Missing serialized easing field: " + field);
            var component = property.objectReferenceValue as ToggleComponent;
            Assert.That(component, Is.Not.Null, "Unwired serialized easing field: " + field);
            return component;
        }

        private static void AssertLabel(ToggleComponent component, string expected)
        {
            var label = new SerializedObject(component).FindProperty("labelText").objectReferenceValue;
            Assert.That(label, Is.Not.Null, component.name + " needs its label reference.");
            Assert.That(new SerializedObject(label).FindProperty("m_text").stringValue, Is.EqualTo(expected));
        }

        // Resolve the layout type through reflection to avoid adding a UnityEngine.UI reference to this test
        // assembly.
        private void RebuildTabLayout()
        {
            for (var parent = tab.transform; parent != null; parent = parent.parent)
                parent.gameObject.SetActive(true);
            var layoutType = Type.GetType("UnityEngine.UI.LayoutRebuilder, UnityEngine.UI", true);
            layoutType.GetMethod("ForceRebuildLayoutImmediate").Invoke(null, new object[] { tab });
        }

        // Use serialized properties to inspect table entries without depending on localization package types.
        private static SerializedProperty FindEntry(SerializedProperty entries, string field, string value)
        {
            for (var i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var property = entry.FindPropertyRelative(field);
                var candidate = property.propertyType == SerializedPropertyType.String
                    ? property.stringValue
                    : property.longValue.ToString();
                if (candidate == value)
                    return entry;
            }
            return null;
        }
    }
}
