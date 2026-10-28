using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>Adds a regular, searchable category using the existing global settings tab, panel and option widgets.</summary>
public static class TempoMapperOptions
{
    public static OptionsTabButton Create(OptionsTabButton templateTab, Canvas templatePanel, Func<string, string> localize = null, Sprite icon = null)
    {
        string Text(string key) => localize != null ? localize(key) :
            LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", "tempo." + key);
        var scene = templateTab.gameObject.scene;
        var dropdownTemplate = UnityEngine.Object.FindObjectsByType<SettingsBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(item => item.gameObject.scene == scene && item.BindedSetting == nameof(Settings.NoteHitSound));
        var toggleTemplate = templatePanel.GetComponentsInChildren<BetterToggle>(true)
            .First(item => item.gameObject.activeSelf && item.Description != null);
        var tab = UnityEngine.Object.Instantiate(templateTab, templateTab.transform.parent, false);
        tab.name = "Tempo Mapper Tab";
        tab.transform.SetSiblingIndex(templateTab.transform.GetSiblingIndex() + 1);
        if (icon != null) tab.Icon.sprite = icon;
        foreach (var image in tab.GetComponentsInChildren<Image>(true))
            if (image != tab.Icon && (image.name == "Corner" || image.name == "Arrow")) image.gameObject.SetActive(false);
        foreach (var localization in tab.GetComponentsInChildren<LocalizeStringEvent>(true)) localization.enabled = false;
        tab.TextMeshTabName.text = Text("title");
        tab.RefreshWidth();
        var titleBinding = tab.gameObject.AddComponent<TempoMapperSettingBinding>();
        titleBinding.Caption = tab.TextMeshTabName;
        titleBinding.LabelKey = "title";
        var panel = UnityEngine.Object.Instantiate(templatePanel, templatePanel.transform.parent, false);
        panel.name = "Tempo Mapper Panel";
        panel.gameObject.SetActive(false);
        panel.enabled = false;
        var scroll = panel.GetComponentInChildren<ScrollRect>(true);
        var content = scroll.content;
        foreach (var child in content.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach (var oldSearch in panel.GetComponentsInChildren<SearchableTab>(true)) UnityEngine.Object.DestroyImmediate(oldSearch);
        var sectionTemplate = templatePanel.GetComponentsInChildren<SearchableSection>(true).First();
        var section = UnityEngine.Object.Instantiate(sectionTemplate, content, false);
        section.name = "Tempo Mapper Options";
        var heading = section.GetComponentsInChildren<TMP_Text>(true).First(text => text.name == "Section Title");
        foreach (var child in section.transform.Cast<Transform>().ToArray())
            if (!heading.transform.IsChildOf(child)) UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach (var localization in heading.GetComponents<LocalizeStringEvent>()) UnityEngine.Object.DestroyImmediate(localization);
        heading.text = Text("title");
        var headingBinding = heading.gameObject.AddComponent<TempoMapperSettingBinding>();
        headingBinding.Caption = heading;
        headingBinding.LabelKey = "title";
        section.ClearOptions();
        section.gameObject.SetActive(true);
        if (section.GetComponent<Canvas>() != null) section.GetComponent<Canvas>().enabled = true;
        var sectionLayout = section.GetComponent<VerticalLayoutGroup>();
        sectionLayout.childControlWidth = sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.padding = new RectOffset(12, 12, 12, 12);
        sectionLayout.spacing = 8;
        var headingSize = heading.GetComponent<LayoutElement>();
        if (headingSize == null) headingSize = heading.gameObject.AddComponent<LayoutElement>();
        headingSize.minHeight = headingSize.preferredHeight = 28;
        void Normalize(GameObject row)
        {
            row.transform.localScale = Vector3.one;
            foreach (var rect in row.GetComponentsInChildren<RectTransform>(true))
                if (Math.Abs(rect.localScale.x) > 1.1f || Math.Abs(rect.localScale.y) > 1.1f) rect.localScale = Vector3.one;
            foreach (var scaler in row.GetComponentsInChildren<CanvasScaler>(true)) UnityEngine.Object.DestroyImmediate(scaler);
            foreach (var raycaster in row.GetComponentsInChildren<GraphicRaycaster>(true)) UnityEngine.Object.DestroyImmediate(raycaster);
            foreach (var canvas in row.GetComponentsInChildren<Canvas>(true)) UnityEngine.Object.DestroyImmediate(canvas);
            var fitter = row.GetComponent<ContentSizeFitter>();
            if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);
            foreach (var graphic in row.GetComponentsInChildren<MaskableGraphic>(true)) graphic.maskable = true;
        }
        GameObject Row(GameObject source, string label)
        {
            var row = UnityEngine.Object.Instantiate(source, section.transform, false);
            row.name = "Tempo " + label;
            Normalize(row);
            var size = row.GetComponent<LayoutElement>();
            if (size == null) size = row.AddComponent<LayoutElement>();
            size.layoutPriority = 100;
            size.minHeight = size.preferredHeight = 32;
            size.flexibleHeight = 0;
            foreach (var binder in row.GetComponentsInChildren<SettingsBinder>(true)) UnityEngine.Object.DestroyImmediate(binder);
            foreach (var localization in row.GetComponentsInChildren<LocalizeStringEvent>(true)) UnityEngine.Object.DestroyImmediate(localization);
            var search = row.GetComponent<SearchableOption>();
            if (search == null) search = row.AddComponent<SearchableOption>();
            search.Keywords = new[] { label, "Tempo Mapper", "tempo", "audio" };
            section.RegisterOption(search);
            var tooltip = row.GetComponent<Tooltip>();
            if (tooltip != null) tooltip.TooltipOverride = label;
            row.SetActive(true);
            return row;
        }
        void Select(string key, string setting, string[] labels, object[] values)
        {
            var label = Text(key);
            // Clone the complete Hit Sounds option: its caption, dropdown and anchors form one styled widget.
            var source = dropdownTemplate.GetComponentInParent<SearchableOption>().gameObject;
            var row = Row(source, label);
            var description = row.GetComponentsInChildren<TMP_Text>(true).First(text => text.name == "Description");
            description.text = label;
            var dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);
            dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            dropdown.ClearOptions();
            dropdown.AddOptions(labels.ToList());
            var binding = row.AddComponent<TempoMapperSettingBinding>();
            binding.SettingName = setting;
            binding.LabelKey = key;
            binding.Caption = description;
            binding.Dropdown = dropdown;
            binding.Values = values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)).ToArray();
        }
        void Toggle(string key, string setting)
        {
            var row = Row(toggleTemplate.gameObject, Text(key));
            var toggle = row.GetComponent<BetterToggle>();
            toggle.Description.text = Text(key);
            toggle.OnValueChanged = new BetterToggle.ToggleEvent();
            var binding = row.AddComponent<TempoMapperSettingBinding>();
            binding.SettingName = setting;
            binding.LabelKey = key;
            binding.Caption = toggle.Description;
            binding.Toggle = toggle;
        }
        Select("timeline.display", nameof(Settings.TempoOverviewDisplay), new[] { Text("waveform"), Text("spectrogram") }, new object[] { 0, 1 });
        Select("editor.display", nameof(Settings.TempoEditorDisplay), new[] { Text("waveform"), Text("spectrogram"), Text("both") }, new object[] { 0, 1, 2 });
        Toggle("waveform.rectify", nameof(Settings.TempoWaveformRectifyPeaks));
        Toggle("follow", nameof(Settings.TempoFollowPlayback));
        var searchable = tab.GetComponent<SearchableTab>();
        if (searchable == null) searchable = tab.gameObject.AddComponent<SearchableTab>();
        searchable.Configure(content, tab.gameObject, section);
        foreach (var search in UnityEngine.Object.FindObjectsByType<SearchInputField>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(item => item.gameObject.scene == scene))
        {
            search.Tabs.RemoveAll(item => item == null);
            search.Tabs.Add(searchable);
        }
        panel.gameObject.SetActive(true);
        return tab;
    }
}
