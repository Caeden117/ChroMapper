using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// basically similar to <see cref="StrobeGeneratorUIDropdown"/>
/// </summary>
public class LightV3GeneratorAppearance : MonoBehaviour
{
    [SerializeField] private RectTransform lightV3GenUIRect;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject rotationPanel;
    [SerializeField] private GameObject translationPanel;
    private RefreshLayoutGroup refresh;
    [SerializeField] private LightColorEventsContainer lightColorEventsContainer;
    internal PlatformDescriptorV3 PlatformDescriptor => lightColorEventsContainer.platformDescriptor;
    private const int thirdCollectionOffset = 3; // maybe we shold set it as a setting later
    public enum LightV3UIPanel
    {
        LightColorPanel,
        LightRotationPanel,
        LightTranslationPanel,
    };
    public Action<LightV3UIPanel> OnToggleUIPanelSwitch;
    private LightV3UIPanel currentPanel = LightV3UIPanel.LightColorPanel;

    public bool IsActive { get; private set; }

    private void Start()
    {
        OnToggleUIPanelSwitch += SwitchColorRotation;
        refresh = GetComponent<RefreshLayoutGroup>();
    }

    private void OnDestroy()
    {
        OnToggleUIPanelSwitch -= SwitchColorRotation;
    }

    public void ToggleDropdown() => ToggleDropdown(!IsActive);

    public void ToggleDropdown(bool visible)
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(UpdateGroup(visible, lightV3GenUIRect));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        IsActive = enabled;
        float dest = enabled ? -150 : 120;
        var og = group.anchoredPosition.x;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(Mathf.Lerp(og, dest, t), group.anchoredPosition.y);
            og = group.anchoredPosition.x;
            yield return new WaitForEndOfFrame();
        }

        group.anchoredPosition = new Vector2(dest, group.anchoredPosition.y);
    }

    public void OnToggleColorRotationSwitch()
    {
        switch (currentPanel)
        {
            case LightV3UIPanel.LightColorPanel:
                currentPanel = LightV3UIPanel.LightRotationPanel;
                break;
            case LightV3UIPanel.LightRotationPanel:
                currentPanel = PlatformDescriptor.HasTranslationEvent ? LightV3UIPanel.LightTranslationPanel : LightV3UIPanel.LightColorPanel;
                break;
            case LightV3UIPanel.LightTranslationPanel:
                currentPanel = LightV3UIPanel.LightColorPanel;
                break;
        }
        OnToggleUIPanelSwitch.Invoke(currentPanel);
        
    }

    public float GetContainerYOffset(LightV3UIPanel requestPanel)
    {
        if (currentPanel == LightV3UIPanel.LightColorPanel)
        {
            switch (requestPanel)
            {
                case LightV3UIPanel.LightRotationPanel:
                    return 0;
                case LightV3UIPanel.LightTranslationPanel:
                    return thirdCollectionOffset;
            }
        }
        else if (currentPanel == LightV3UIPanel.LightRotationPanel)
        {
            switch (requestPanel)
            {
                case LightV3UIPanel.LightColorPanel:
                    return PlatformDescriptor.HasTranslationEvent ? thirdCollectionOffset : 0;
                case LightV3UIPanel.LightTranslationPanel:
                    return 0;
            }
        }
        else if (currentPanel == LightV3UIPanel.LightTranslationPanel)
        {
            switch (requestPanel)
            {
                case LightV3UIPanel.LightColorPanel:
                    return 0;
                case LightV3UIPanel.LightRotationPanel:
                    return thirdCollectionOffset;
            }
        }
        return 0;
    }

    public int GetTotalLightCount<TBo>(TBo x)
    {
        try
        {
            switch (x)
            {
                case BeatmapLightColorEvent colorEvent:
                    return PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(colorEvent.Group)].ControllingLights.Count;
                case BeatmapLightRotationEvent rotationEvent:
                    return PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(rotationEvent.Group)].ControllingRotations.Count;
                case BeatmapLightTranslationEvent translationEvent:
                    return PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(translationEvent.Group)].ControllingTranslations.Count;
            }
        }
        catch { }
        return 0;
    }

    public int GetFilteredLightCount<TBo>(TBo x)
    {
        try
        {
            switch (x)
            {
                case BeatmapLightColorEvent colorEvent:
                    return colorEvent.EventBoxes[0].Filter.Filter(
                        PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(colorEvent.Group)]
                        .ControllingLights).Count();
                case BeatmapLightRotationEvent rotationEvent:
                    return rotationEvent.EventBoxes[0].Filter.Filter(
                        PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(rotationEvent.Group)]
                        .ControllingRotations).Count();
                case BeatmapLightTranslationEvent translationEvent:
                    return translationEvent.EventBoxes[0].Filter.Filter(
                        PlatformDescriptor.LightsManagersV3[PlatformDescriptor.GroupIdToLaneIndex(translationEvent.Group)]
                        .ControllingTranslations).Count();
            }
        }
        catch { }
        return 0;
    }

    private void SwitchColorRotation(LightV3UIPanel currentPanel)
    {
        colorPanel.SetActive(currentPanel == LightV3UIPanel.LightColorPanel);
        rotationPanel.SetActive(currentPanel == LightV3UIPanel.LightRotationPanel);
        translationPanel.SetActive(currentPanel == LightV3UIPanel.LightTranslationPanel);

        refresh.TriggerRefresh();
    }
}
