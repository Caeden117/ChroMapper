using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GLSGroupPageViewController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;

    [SerializeField] private ButtonComponent textPrefab;
    [SerializeField] private RectTransform targetTransform;

    private readonly List<ButtonComponent> loadedText = new();
    private readonly Dictionary<string, ButtonComponent> groupToText = new();

    private void Start()
    {
        beatmapRuntimeContext.OnTrackDefinitionsChanged += HandleTrackDefinitionsChanged;
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsGroupGridProvider.OnGroupPageChanged += HandleGroupPageChanged;

        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy()
    {
        beatmapRuntimeContext.OnTrackDefinitionsChanged -= HandleTrackDefinitionsChanged;
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsGroupGridProvider.OnGroupPageChanged -= HandleGroupPageChanged;
    }

    private void HandleTrackDefinitionsChanged(TrackDefinitionsSO td)
    {
        foreach (var text in loadedText) Destroy(text.gameObject);
        loadedText.Clear();
        groupToText.Clear();

        foreach (var n in td.Gls.Values.Select(g => g.Group).Distinct())
        {
            var text = Instantiate(textPrefab, targetTransform);
            text.name = n;
            text.SetLabelText(n);
            text.OnClick(() => glsGroupGridProvider.SetGroupPage(n));
            // The button's child selectable receives hover events, so host the remappable hint there rather than on the component root.
            var tooltipTarget = text.Selectable != null ? text.Selectable.gameObject : text.gameObject;
            var tooltip = tooltipTarget.GetComponent<Tooltip>() ?? tooltipTarget.AddComponent<Tooltip>();
            // TODO: Localize this tooltip before Stable so the new remappable hint follows the rest of the UI.
            tooltip.TooltipOverride = "Cycle GLS tabs";
            tooltip.AdvancedTooltip = "Cycle GLS tabs";
            tooltip.AppearDelay = 0.3f;
            tooltip.HotkeyActionMap = "GLS Group Tabs";
            tooltip.HotkeyActionName = "Next Groups Page";
            tooltip.AdditionalHotkeyActionName = "Previous Groups Page";
            text.gameObject.SetActive(true);
            loadedText.Add(text);
            groupToText.Add(n, text);
        }

        HandleGroupPageChanged(glsGroupGridProvider.CurrentGroup);
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        canvasGroup.alpha = mode.HasFlag(EditingMode.GLS) ? 1 : 0;
        canvasGroup.blocksRaycasts = mode.HasFlag(EditingMode.GLS);
    }

    private void HandleGroupPageChanged(string group)
    {
        foreach (var t in loadedText) t.SetLabelColor(new(0.25f, 0.25f, 0.25f));
        if (!groupToText.TryGetValue(glsGroupGridProvider.CurrentGroup, out var text)) return;
        text.SetLabelColor(Color.white);
    }
}
