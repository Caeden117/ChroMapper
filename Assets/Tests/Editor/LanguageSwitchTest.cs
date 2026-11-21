using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class LanguageSwitchTest
    {
        private const string MissingTranslation = "No translation found for";
        private const string OptionsScene = "04_Options";
        private const string SongSelectScene = "01_SongSelectMenu";

        private readonly Dictionary<string, SortedSet<string>> failures = new();
        private Locale previousLocale;
        private string previousLanguage;
        private bool previousInstantTransitions;
        private GLSEventGridProvider gridProvider;
        private EditModeContext editMode;

        [UnityTest]
        public IEnumerator AllLanguagesAcrossUi()
        {
            yield return TestUtils.LoadMap(3);
            yield return LocalizationSettings.InitializationOperation;
            previousLocale = LocalizationSettings.SelectedLocale;
            previousLanguage = Settings.Instance.Language;
            previousInstantTransitions = Settings.Instance.InstantEscapeMenuTransitions;
            Settings.Instance.InstantEscapeMenuTransitions = true;

            editMode = Object.FindAnyObjectByType<EditModeContext>();
            gridProvider = Object.FindAnyObjectByType<GLSEventGridProvider>(FindObjectsInactive.Include);
            editMode.EditingMode = EditingMode.GLS;
            var innerGroups = new BaseEventBoxGroup[]
            {
                new BaseLightColorEventBoxGroup { Boxes = { new BaseLightColorEventBox() } },
                new BaseLightRotationEventBoxGroup { Boxes = { new BaseLightRotationEventBox() } },
                new BaseLightTranslationEventBoxGroup { Boxes = { new BaseLightTranslationEventBox() } },
                new BaseVfxEventEventBoxGroup { Boxes = { new BaseVfxEventEventBox() } }
            };

            OptionsController.ShowOptions();
            yield return new WaitUntil(() => SceneManager.GetSceneByName(OptionsScene).isLoaded);
            yield return null;
            var options = Object.FindAnyObjectByType<OptionsController>();
            var language = Object.FindAnyObjectByType<LanguageDropdownSettingsBinder>();
            var tabs = Object.FindObjectsByType<OptionsTabButton>(FindObjectsSortMode.None);
            var general = tabs.Single(tab => tab.name == "General Tab");
            var title = general.GetComponentInParent<TabManager>().transform.Find("Title").GetComponent<TMP_Text>();
            var locales = LocalizationSettings.AvailableLocales.Locales.ToArray();
            Assert.That(locales.Length, Is.GreaterThan(1));
            var dropdownDeadline = Time.realtimeSinceStartup + 10;
            yield return new WaitUntil(() => language.Dropdown.options.Count == locales.Length
                || Time.realtimeSinceStartup >= dropdownDeadline);
            Assert.That(language.Dropdown.options.Count, Is.EqualTo(locales.Length));

            general.ChangeTab();
            foreach (var code in new[] { "nl", "en", "fr" })
            {
                var index = Array.FindIndex(locales, locale => locale.Identifier.Code == code);
                yield return SwitchLocale(locales[index], () => language.Dropdown.value = index);
                CheckSettingsTitle(title, general, code);
            }

            // Exercise a live English-to-Chinese switch over GLS before visiting the other languages.
            var localeIndices = Enumerable.Range(0, locales.Length)
                .OrderBy(index => locales[index].Identifier.Code switch { "en" => 0, "zh-CN" => 1, _ => 2 });
            foreach (var index in localeIndices)
            {
                var locale = locales[index];
                var code = locale.Identifier.Code;
                editMode.EditingMode = EditingMode.GLS;
                general.ChangeTab();
                yield return SwitchLocale(locale, () => language.Dropdown.value = index);
                CheckSettingsTitle(title, general, code);
                yield return CheckUi(code, "GLS with General open", text => text.scene.name != OptionsScene);

                foreach (var tab in tabs)
                {
                    tab.ChangeTab();
                    yield return CheckUi(code, tab.name, text => text.scene.name == OptionsScene);
                    CheckSettingsTitle(title, tab, code);
                }

                editMode.EditingMode = EditingMode.Gameplay;
                yield return CheckUi(code, "Gameplay", text => text.scene.name != OptionsScene);
                editMode.EditingMode = EditingMode.BasicEvent;
                yield return CheckUi(code, "Basic Events", text => text.scene.name != OptionsScene);
                editMode.EditingMode = EditingMode.EventBox;
                foreach (var group in innerGroups)
                {
                    gridProvider.GroupContext = group;
                    yield return CheckUi(code, $"GLS {group.GetType().Name}", text => text.scene.name != OptionsScene);
                }
                editMode.EditingMode = EditingMode.GLS;
                gridProvider.GroupContext = null;
            }

            options.Close();
            yield return new WaitUntil(() => !SceneManager.GetSceneByName(OptionsScene).isLoaded);
            editMode.EditingMode = EditingMode.Gameplay;
            var pause = Object.FindAnyObjectByType<PauseManager>();
            pause.TogglePause();
            foreach (var locale in locales)
            {
                yield return SwitchLocale(locale, () => LocalizationSettings.SelectedLocale = locale);
                yield return CheckUi(locale.Identifier.Code, "Pause menu", _ => true);
            }
            pause.TogglePause();

            foreach (var scene in new[] { "02_SongEditMenu", SongSelectScene })
            {
                yield return LoadScene(scene);
                foreach (var locale in locales)
                {
                    yield return SwitchLocale(locale, () => LocalizationSettings.SelectedLocale = locale);
                    yield return CheckUi(locale.Identifier.Code, scene, _ => true);
                }
            }
            yield return CheckFirstBoot(locales);

            if (failures.Count != 0)
            {
                var report = failures.OrderBy(failure => failure.Key, StringComparer.Ordinal)
                    .Select(failure => $"{failure.Key}\n  {string.Join(", ", failure.Value)}");
                Assert.Fail($"Found {failures.Count} UI localization failures:\n{string.Join("\n", report)}");
            }
        }

        private void CheckSettingsTitle(TMP_Text title, OptionsTabButton tab, string locale)
        {
            var tabName = tab.TextMeshTabName.GetComponent<LocalizeStringEvent>().StringReference.GetLocalizedString();
            var expected = tab.name == "Credits Tab"
                ? tabName
                : LocalizationSettings.StringDatabase.GetLocalizedString(
                    "Options", "heading", new object[] { new { TabName = tabName } });
            if (!string.Equals(title.text, expected, StringComparison.Ordinal))
                RecordFailure(title, $"Expected '{expected}', displayed '{title.text}'", locale, tab.name);
        }

        private IEnumerator CheckFirstBoot(Locale[] locales)
        {
            const string firstBootScene = "00_FirstBoot";
            var settings = Settings.Instance;
            var installation = settings.BeatSaberInstallation;
            try
            {
                settings.BeatSaberInstallation = PathUtils.Combine(Settings.TestRunnerInstallationDirectory, "MissingInstallation");
                yield return LoadScene(firstBootScene);
                var firstBoot = Object.FindAnyObjectByType<FirstBootMenu>();
                foreach (var locale in locales)
                {
                    yield return SwitchLocale(locale, () => LocalizationSettings.SelectedLocale = locale);
                    yield return CheckUi(locale.Identifier.Code, "First boot", _ => true);
                    firstBoot.ToggleHelp();
                    yield return CheckUi(locale.Identifier.Code, "First boot help", _ => true);
                    firstBoot.ToggleHelp();
                }
            }
            finally
            {
                settings.BeatSaberInstallation = installation;
            }
            yield return LoadScene(SongSelectScene);
        }

        private static IEnumerator LoadScene(string scene)
        {
            SceneTransitionManager.Instance.LoadScene(scene);
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        }

        [UnityTearDown]
        public IEnumerator RestoreUi()
        {
            var options = Object.FindAnyObjectByType<OptionsController>();
            if (options != null)
            {
                options.Close();
                yield return new WaitUntil(() => !SceneManager.GetSceneByName(OptionsScene).isLoaded);
            }

            var pause = Object.FindAnyObjectByType<PauseManager>();
            if (pause != null && PauseManager.IsPaused)
                pause.TogglePause();
            if (editMode != null)
                editMode.EditingMode = EditingMode.Gameplay;
            if (gridProvider != null)
                gridProvider.GroupContext = null;
            if (previousLocale != null)
            {
                LocalizationSettings.SelectedLocale = previousLocale;
                Settings.Instance.Language = previousLanguage;
                Settings.Instance.InstantEscapeMenuTransitions = previousInstantTransitions;
                yield return LocalizationSettings.InitializationOperation;
            }
            TestUtils.ReturnSettings();
        }

        private static IEnumerator SwitchLocale(Locale locale, Action select)
        {
            var notified = LocalizationSettings.SelectedLocale == locale;
            void OnLocaleChanged(Locale selected) => notified = selected == locale;

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            try
            {
                select();
                var deadline = Time.realtimeSinceStartup + 10;
                yield return new WaitUntil(() => notified || Time.realtimeSinceStartup >= deadline);
                Assert.That(notified, Is.True, $"Locale {locale.Identifier.Code} did not finish switching.");
            }
            finally
            {
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            }
        }

        private IEnumerator CheckUi(string locale, string area, Predicate<GameObject> includes)
        {
            // Localized labels can still be loading after the locale-changed event or a panel becomes active.
            yield return null;
            var localizers = Object.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None);
            var deadline = Time.realtimeSinceStartup + 10;
            while (localizers.Any(localizer => localizer != null && localizer.isActiveAndEnabled
                && includes(localizer.gameObject) && IsLoading(localizer)) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            foreach (var localizer in localizers)
            {
                if (localizer != null && localizer.isActiveAndEnabled && includes(localizer.gameObject)
                    && IsLoading(localizer))
                {
                    RecordFailure(localizer, "Localized text did not finish loading", locale, area);
                }
            }
            yield return null;

            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (text.isActiveAndEnabled)
                    Record(text, text.text, locale, area, includes);
            }
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (text.isActiveAndEnabled)
                    Record(text, text.text, locale, area, includes);
            }
        }

        private static bool IsLoading(LocalizeStringEvent localizer)
        {
            var operation = localizer.StringReference.CurrentLoadingOperationHandle;
            return operation.IsValid() && !operation.IsDone;
        }

        private void Record(Component text, string value, string locale, string area, Predicate<GameObject> includes)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains(MissingTranslation, StringComparison.OrdinalIgnoreCase)
                || !includes(text.gameObject) || text.GetComponentsInParent<Canvas>().Any(canvas => !canvas.enabled))
            {
                return;
            }

            RecordFailure(text, value, locale, area);
        }

        private void RecordFailure(Component component, string value, string locale, string area)
        {
            var path = component.name;
            for (var parent = component.transform.parent; parent != null; parent = parent.parent)
                path = $"{parent.name}/{path}";

            var location = $"{component.gameObject.scene.name}/{path} — {value}";
            if (!failures.TryGetValue(location, out var occurrences))
                failures.Add(location, occurrences = new SortedSet<string>(StringComparer.Ordinal));
            occurrences.Add($"{locale} ({area})");
        }
    }
}
