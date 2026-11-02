using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventBoxViewController : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GameObject targetObject;

    [Header("Event Box Tool")] [SerializeField]
    private ButtonComponent addEventBoxButton;

    [SerializeField] private ButtonComponent addIdsEventBoxButton;
    [SerializeField] private ButtonComponent addAxesEventBoxButton;
    [SerializeField] private ButtonComponent addIdsAndAxesEventBoxButton;

    [SerializeField] private ButtonComponent deleteEventBoxButton;
    [SerializeField] private ButtonComponent deletePruneEventBoxButton;

    [SerializeField] private ButtonComponent sortIdsEventBoxButton;
    [SerializeField] private ButtonComponent sortAxesEventBoxButton;

    [SerializeField] private ButtonComponent moveDownEventBoxButton;
    [SerializeField] private ButtonComponent moveUpEventBoxButton;
    [SerializeField] private ButtonComponent duplicateEventBoxButton;

    [Header("Placement Scripts")] [SerializeField]
    private GLSEventColorPlacement colorPlacement;

    [SerializeField] private GLSEventRotationPlacement rotationPlacement;
    [SerializeField] private GLSEventTranslationPlacement translationPlacement;
    [SerializeField] private GLSEventFloatFXPlacement floatFXPlacement;

    [Header("ID Tab")] [SerializeField] private ToggleComponent idTabPrefab;
    [SerializeField] private RectTransform idTabTargetTransform;
    private readonly List<ToggleComponent> instantiatedIdTab = new();

    [Header("Info Text")] [SerializeField] private TextComponent eventBoxIdText;
    [SerializeField] private TextComponent filteredIdText;
    [SerializeField] private Image idImagePrefab;
    [SerializeField] private Transform idImageTargetTransform;
    private readonly List<Image> instantiatedIdImage = new();

    [SerializeField] private TextMeshProUGUI errorTextPrefab;
    [SerializeField] private Transform errorTextTargetTransform;
    private readonly List<TextMeshProUGUI> instantiatedErrorText = new();

    [Header("Input")] [SerializeField] private GameObject inputContainer;
    [Space] [SerializeField] private ToggleComponent beatDistributionWaveToggle;
    [SerializeField] private ToggleComponent beatDistributionStepToggle;
    [SerializeField] private TextBoxFloatComponent beatDistributionInput;
    [Space] [SerializeField] private ToggleComponent filterTypeSectionToggle;
    [SerializeField] private ToggleComponent filterTypeStepToggle;
    [SerializeField] private TextBoxIntComponent chunkInput;
    [SerializeField] private ToggleComponent reverseToggle;
    [SerializeField] private TextBoxIntComponent p0Input;
    [SerializeField] private ButtonComponent splitSectionsButton;
    [SerializeField] private TextBoxIntComponent p1Input;
    [SerializeField] private ToggleComponent randomToggle;
    [SerializeField] private ToggleComponent inOrderToggle;
    [SerializeField] private TextBoxIntComponent seedInput;
    [SerializeField] private ButtonComponent randomizeSeedButton;
    [SerializeField] private ButtonComponent resetSeedButton;
    [SerializeField] private GameObject axisObject;
    [SerializeField] private ToggleComponent axisXToggle;
    [SerializeField] private ToggleComponent axisYToggle;
    [SerializeField] private ToggleComponent axisZToggle;
    [SerializeField] private ToggleComponent flipToggle;
    [Space] [SerializeField] private TextBoxFloatComponent limitInput;
    [SerializeField] private ToggleComponent limitDurationToggle;
    [SerializeField] private ToggleComponent limitDistributionToggle;
    [Space] [SerializeField] private ToggleComponent valueDistributionWaveToggle;
    [SerializeField] private ToggleComponent valueDistributionStepToggle;
    [SerializeField] private TextBoxFloatComponent valueDistributionInput;
    [SerializeField] private ToggleComponent affectFirstToggle;
    [SerializeField] private DropdownComponent easeTypeDropdown;

    [SerializeField] private GLSColorDistributionArrayViewController colorDistributionsView;
    [SerializeField] private GLSColorDistributionArrayViewController strobeColorDistributionsView;

    private BaseEventBoxGroup groupContext;
    private BaseEventBox boxContext;
    private int boxIndex;
    private int displayedBoxIndex;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        glsEventGridProvider.OnAutomaticLaneDefaultsChanged += HandleAutomaticLaneDefaultsChanged;
        addEventBoxButton.OnClick(HandleAddEventBox);
        addIdsEventBoxButton.OnClick(HandleAddIdsEventBox);
        addAxesEventBoxButton.OnClick(HandleAddAxesEventBox);
        addIdsAndAxesEventBoxButton.OnClick(HandleAddIdsAndAxesEventBox);
        deleteEventBoxButton.OnClick(HandleDeleteEventBox);
        deletePruneEventBoxButton.OnClick(HandleDeletePruneEventBox);
        sortIdsEventBoxButton.OnClick(HandleSortIdsEventBox);
        sortAxesEventBoxButton.OnClick(HandleSortAxesEventBox);
        moveDownEventBoxButton.OnClick(HandleMoveDownEventBox);
        moveUpEventBoxButton.OnClick(HandleMoveUpEventBox);
        duplicateEventBoxButton.OnClick(HandleDuplicateEventBox);
        splitSectionsButton.OnClick(HandleSplitSections);

        // Explain where the new lane is inserted and that the first matching box controls each light.
        const string addLaneTooltip =
            "Add a lane following the currently selected event box lane below. Lights are owned by the first lane whose filter matches them.";
        AddTooltip(addEventBoxButton, addLaneTooltip, addLaneTooltip);
        AddTooltip(addIdsEventBoxButton,
            "Add IDs",
            "DESTRUCTIVE — clears all existing boxes and generates one box per light ID in this group (Step filter, one ID each), giving you per-light granular control; all existing node data will be lost.");
        AddTooltip(addAxesEventBoxButton,
            "Add Axes",
            "DESTRUCTIVE (rotation/translation only) — clears all existing boxes and generates one box per available axis (X/Y/Z) so each axis can have its own distribution; not applicable to color groups and all existing node data will be lost.");
        AddTooltip(addIdsAndAxesEventBoxButton,
            "Add Axes & IDs",
            "DESTRUCTIVE — clears all existing boxes and generates one box for every axis/light-ID combination, providing maximum per-light per-axis granularity; all existing node data will be lost.");
        AddTooltip(deleteEventBoxButton,
            "Delete Box (X)",
            "Permanently deletes the currently selected event box and all of its nodes — use Ctrl+Z to undo.");
        AddTooltip(deletePruneEventBoxButton,
            "Prune",
            "Removes every event box that contains zero nodes, cleaning up empty placeholder boxes while leaving any box that has at least one node fully intact.");
        AddTooltip(sortIdsEventBoxButton,
            "Sort IDs",
            "Reorders all boxes in ascending order by their index-filter starting ID, making the box list easier to read and navigate when IDs were added out of order.");
        AddTooltip(sortAxesEventBoxButton,
            "Sort Axes",
            "Reorders all boxes so that X-axis boxes come first, then Y, then Z, making rotation and translation groups easier to read; has no effect on color groups.");
        AddTooltip(duplicateEventBoxButton,
            "Dupe",
            "Creates an exact copy of the currently selected box — including its filter settings and all of its nodes — and inserts it immediately after the original.");
        AddTooltip(moveUpEventBoxButton,
            "Move Up",
            "Moves the currently selected box one slot earlier in the list, which affects playback order when boxes share overlapping IDs and distributions.");
        AddTooltip(moveDownEventBoxButton,
            "Move Down",
            "Moves the currently selected box one slot later in the list, which affects playback order when boxes share overlapping IDs and distributions.");

        beatDistributionWaveToggle.OnValueChanged(HandleBeatDistributionWaveValueChanged);
        beatDistributionStepToggle.OnValueChanged(HandleBeatDistributionStepValueChanged);
        beatDistributionInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentTimePrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollTime)
            .OnEndEdit(HandleBeatDistributionValueChanged)
            .OnValueChanged(HandleBeatDistributionValueChanged);
        filterTypeSectionToggle.OnValueChanged(HandleFilterTypeSectionValueChanged);
        filterTypeStepToggle.OnValueChanged(HandleFilterTypeStepValueChanged);
        chunkInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleChunkValueChanged)
            .OnValueChanged(HandleChunkValueChanged);
        reverseToggle.OnValueChanged(HandleReverseValueChanged);
        p0Input
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEditingValueChanged(HandleParam0EditingValueChanged)
            .OnEndEdit(HandleParam0ValueChanged)
            .OnValueChanged(HandleParam0ValueChanged);
        p1Input
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleParam1ValueChanged)
            .OnValueChanged(HandleParam1ValueChanged);
        randomToggle.OnValueChanged(HandleRandomValueChanged);
        inOrderToggle.OnValueChanged(HandleInOrderValueChanged);
        seedInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleSeedValueChanged)
            .OnValueChanged(HandleSeedValueChanged);
        randomizeSeedButton.OnClick(HandleRandomizeSeed);
        resetSeedButton.OnClick(HandleResetSeed);
        axisXToggle.OnValueChanged(HandleAxisXValueChanged);
        axisYToggle.OnValueChanged(HandleAxisYValueChanged);
        axisZToggle.OnValueChanged(HandleAxisZValueChanged);
        flipToggle.OnValueChanged(HandleFlipValueChanged);
        limitInput
            .WithScrollPrecision(scrollPrecisionController.GetCurrentPercentPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleLimitValueChanged)
            .OnValueChanged(HandleLimitValueChanged);
        limitDurationToggle.OnValueChanged(HandleLimitDurationValueChanged);
        limitDistributionToggle.OnValueChanged(HandleLimitDistributionValueChanged);
        valueDistributionWaveToggle.OnValueChanged(HandleValueDistributionWaveValueChanged);
        valueDistributionStepToggle.OnValueChanged(HandleValueDistributionStepValueChanged);
        valueDistributionInput
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleValueDistributionValueChanged)
            .OnValueChanged(HandleValueDistributionValueChanged);
        affectFirstToggle.OnValueChanged(HandleAffectFirstValueChanged);
        easeTypeDropdown.WithOptions(Easing.IDToFullName.Values).OnValueChanged(HandleEaseTypeValueChanged);

        colorDistributionsView.Initialize(colorDistributions =>
            GLSEventBoxCommand.SetColorDistributions(colorDistributions, false, groupContext, boxIndex));
        strobeColorDistributionsView.Initialize(strobeColorDistributions =>
            GLSEventBoxCommand.SetColorDistributions(strobeColorDistributions, true, groupContext, boxIndex));

        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy()
    {
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        targetObject.SetActive(mode.HasFlag(EditingMode.EventBox));
        if (!editModeContext.IsTemporaryModeChange && !mode.HasFlag(EditingMode.EventBox))
            SetBoxIndex(0);
    }

    private void HandleAutomaticLaneDefaultsChanged()
    {
        if (boxContext is BaseLightTransformEventBox { IsAutomaticAxisLane: true } box)
        {
            affectFirstToggle.SetValueWithoutNotify(box.AffectFirst == 1);
        }
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        groupContext = group;
        boxContext = null;
        ConfigureAxisActions(groupContext);
        // Parent replacement clears the active GLS context before its clone is installed, so defer box UI work until a group exists.
        if (groupContext == null)
        {
            boxIndex = -1;
            inputContainer.SetActive(false);
            colorDistributionsView.gameObject.SetActive(false);
            strobeColorDistributionsView.gameObject.SetActive(false);
            return;
        }

        SetBoxIndex(displayedBoxIndex);
    }

    private void ConfigureAxisActions(BaseEventBoxGroup group)
    {
        // FloatFX and color boxes do not implement the shared transform-group contract used by axis-only operations.
        var supportsAxes = group is ILightTransformEventBoxGroup;
        addAxesEventBoxButton.Selectable.interactable = supportsAxes;
        addIdsAndAxesEventBoxButton.Selectable.interactable = supportsAxes;
        sortAxesEventBoxButton.Selectable.interactable = supportsAxes;
    }

    private Action<bool> HandleSetBoxIndex(int id)
    {
        return _ =>
        {
            SetBoxIndex(id);
        };
    }

    private void HandleAddEventBox()
    {
        if (groupContext == null)
            return;
        if (boxIndex < 0 && boxContext != null)
        {
            MaterializeSelectedLane();
            return;
        }
        var targetIndex = boxIndex + 1;
        if (GLSEventBoxCommand.AddEventBox(groupContext, targetIndex) != null)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(targetIndex));
        }
    }

    private void HandleAddIdsEventBox()
    {
        if (groupContext == null)
            return;
        GLSEventBoxCommand.AddAllIdsEventBox(groupContext, GetGroupSize(groupContext));
    }

    private void HandleAddAxesEventBox()
    {
        // Axis generation is meaningful only for groups implementing the shared transform contract.
        if (groupContext is not ILightTransformEventBoxGroup)
            return;
        var td = beatmapRuntimeContext.TrackDefinitions.GetGlsOrDefault(groupContext.ID);
        GLSEventBoxCommand.AddAllAxesEventBox(groupContext, td);
    }

    private void HandleAddIdsAndAxesEventBox()
    {
        // Axis generation is meaningful only for groups implementing the shared transform contract.
        if (groupContext is not ILightTransformEventBoxGroup)
            return;
        var td = beatmapRuntimeContext.TrackDefinitions.GetGlsOrDefault(groupContext.ID);
        GLSEventBoxCommand.AddAllAxesAndIdsEventBox(groupContext, td, GetGroupSize(groupContext));
    }

    private void HandleDeleteEventBox()
    {
        if (groupContext == null)
            return;
        if (boxIndex >= 0)
        {
            GLSEventBoxCommand.DeleteEventBox(groupContext, boxIndex);
        }
    }

    private void HandleDeletePruneEventBox()
    {
        if (groupContext == null)
            return;
        GLSEventBoxCommand.DeletePruneEventBox(groupContext);
    }

    private void HandleSortIdsEventBox()
    {
        if (groupContext == null)
            return;
        GLSEventBoxCommand.SortIdsEventBox(groupContext);
    }

    private void HandleSortAxesEventBox()
    {
        if (groupContext == null)
            return;
        GLSEventBoxCommand.SortAxesEventBox(groupContext);
    }

    private void HandleMoveDownEventBox()
    {
        if (groupContext == null)
            return;
        if (boxIndex < 0)
        {
            return;
        }
        var targetIndex = boxIndex + 1;
        if (GLSEventBoxCommand.MoveDownEventBox(groupContext, boxIndex) != null)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(targetIndex));
        }
    }

    private void HandleMoveUpEventBox()
    {
        if (groupContext == null)
            return;
        if (boxIndex < 0)
        {
            return;
        }
        var targetIndex = boxIndex - 1;
        if (GLSEventBoxCommand.MoveUpEventBox(groupContext, boxIndex) != null)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(targetIndex));
        }
    }

    private void HandleDuplicateEventBox()
    {
        if (groupContext == null)
            return;
        if (boxIndex < 0)
        {
            MaterializeSelectedLane();
            return;
        }
        var targetIndex = boxIndex + 1;
        if (GLSEventBoxCommand.DuplicateEventBox(groupContext, boxIndex) != null)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(targetIndex));
        }
    }

    public void HandleApplyToSelected()
    {
        Debug.Log("[EventBoxViewController] HandleApplyToSelected called");
        // Buffer typed selection and groups once so painting does not copy or lazily enumerate selection per phase.
        var selectedGlsEvents = new List<BaseGLSEvent>(SelectionController.SelectedObjects.Count);
        foreach (var selectedObject in SelectionController.SelectedObjects)
        {
            if (selectedObject is not BaseGLSEvent selectedGlsEvent)
            {
                Debug.Log("[EventBoxViewController] Ignoring apply because the current selection is not exclusively paintable GLS nodes.");
                return;
            }

            selectedGlsEvents.Add(selectedGlsEvent);
        }

        Debug.Log($"[EventBoxViewController] Found {selectedGlsEvents.Count} selected GLS events");
        if (selectedGlsEvents.Count == 0)
            return;

        var byGroup = new Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>>();
        foreach (var selectedGlsEvent in selectedGlsEvents)
        {
            var group = selectedGlsEvent.EventBoxGroupData;
            if (group == null)
            {
                Debug.LogError($"[PaintProperties] Cannot apply properties to a selected GLS node without an event box group. {Beatmap.BeatmapLogContext.Describe(selectedGlsEvent)}");
                continue;
            }

            if (!byGroup.TryGetValue(group, out var groupEvents))
            {
                groupEvents = new List<BaseGLSEvent>();
                byGroup.Add(group, groupEvents);
            }

            groupEvents.Add(selectedGlsEvent);
        }

        Debug.Log($"[EventBoxViewController] Grouped into {byGroup.Count} groups");
        foreach (var groupEntry in byGroup)
        {
            var oldGroup = groupEntry.Key;
            var groupEvents = groupEntry.Value;
            Debug.Log($"[EventBoxViewController] Processing group with {groupEvents.Count} events");
            var newGroup = BeatmapFactory.Clone(oldGroup);
            var eventIndex = new GLSEventLookupIndex(oldGroup);

            foreach (var evt in groupEvents)
            {
                // Resolve cloned events through their authoritative source position, including stacked duplicates.
                if (!eventIndex.TryGetCloneEvent(evt, newGroup, out var location, out var newEvt))
                {
                    Debug.LogError(
                        $"[PaintProperties] Cannot resolve selected GLS node. {Beatmap.BeatmapLogContext.Describe(evt)}");
                    continue;
                }

                Debug.Log($"[EventBoxViewController] Processing event type {evt.GetType().Name}, newEvt type {newEvt.GetType().Name}");

                switch (newEvt)
                {
                    case BaseLightColorBase color when colorPlacement != null:
                        Debug.Log($"[EventBoxViewController] Applying color properties: brightness={colorPlacement.QueuedData.Brightness}");
                        // Apply only non-color UI values so the replaced node keeps its existing GLS color payload.
                        color.Brightness = colorPlacement.QueuedData.Brightness;
                        color.Easing = colorPlacement.QueuedData.Easing;
                        color.Frequency = colorPlacement.QueuedData.Frequency;
                        color.StrobeBrightness = colorPlacement.QueuedData.StrobeBrightness;
                        color.StrobeFade = colorPlacement.QueuedData.StrobeFade;
                        PreserveColorPayload((BaseLightColorBase)evt, color);
                        color.ColorDistributions = colorPlacement.QueuedData.ColorDistributions.ToArray();
                        color.StrobeColorDistributions = colorPlacement.QueuedData.StrobeColorDistributions.ToArray();
                        color.WriteCustom();
                        break;
                    case BaseLightColorBase color when colorPlacement == null:
                        Debug.LogWarning($"[EventBoxViewController] Color event but colorPlacement is null. {Beatmap.BeatmapLogContext.Describe(evt)}");
                        break;
                    case BaseLightRotationBase rotation when rotationPlacement != null:
                        // Log both source and queued rotation values to diagnose mismatched GLS rotation painting.
                        Debug.Log(
                            $"[PaintProperties] Rotation: groupId={oldGroup.ID}, boxIndex={evt.BoxIndex}, " +
                            $"eventIndex={location.EventIndex}, source={((BaseLightRotationBase)evt).Rotation}, " +
                            $"queued={rotationPlacement.QueuedData.Rotation}, loop={rotationPlacement.QueuedData.Loop}, " +
                            $"ease={rotationPlacement.QueuedData.Easing}.");
                        // Rotation paint should update the editor-selected values without changing the node's existing spin direction choice.
                        rotation.Rotation = rotationPlacement.QueuedData.Rotation;
                        rotation.Loop = rotationPlacement.QueuedData.Loop;
                        rotation.Easing = rotationPlacement.QueuedData.Easing;
                        break;
                    case BaseLightRotationBase rotation when rotationPlacement == null:
                        Debug.LogWarning($"[EventBoxViewController] Rotation event but rotationPlacement is null. {Beatmap.BeatmapLogContext.Describe(evt)}");
                        break;
                    case BaseLightTranslationBase translation when translationPlacement != null:
                        Debug.Log($"[EventBoxViewController] Applying translation properties");
                        translation.Translation = translationPlacement.QueuedData.Translation;
                        translation.Easing = translationPlacement.QueuedData.Easing;
                        break;
                    case BaseLightTranslationBase translation when translationPlacement == null:
                        Debug.LogWarning($"[EventBoxViewController] Translation event but translationPlacement is null. {Beatmap.BeatmapLogContext.Describe(evt)}");
                        break;
                    case BaseFxEventFloat floatFx when floatFXPlacement != null:
                        Debug.Log($"[EventBoxViewController] Applying floatFX properties");
                        floatFx.Value = floatFXPlacement.QueuedData.Value;
                        floatFx.Easing = floatFXPlacement.QueuedData.Easing;
                        break;
                    case BaseFxEventFloat floatFx when floatFXPlacement == null:
                        Debug.LogWarning($"[EventBoxViewController] FloatFX event but floatFXPlacement is null. {Beatmap.BeatmapLogContext.Describe(evt)}");
                        break;
                    default:
                        Debug.LogWarning($"[EventBoxViewController] Event type {newEvt.GetType().Name} not handled by apply logic. {Beatmap.BeatmapLogContext.Describe(evt)}");
                        break;
                }
            }

            GLSCommonCommand.TriggerPlaceAction(oldGroup, newGroup);
        }
    }

    private static void PreserveColorPayload(BaseLightColorBase originalEvent, BaseLightColorBase replacementEvent)
    {
        // Preserve the full original custom payload so replacement-in-place does not drop chroma color, strobe color, or user-authored extras.
        var originalCustomData = originalEvent.CustomData?.Clone() ?? new JSONObject();
        replacementEvent.Color = originalEvent.Color;
        replacementEvent.UsePrevious = originalEvent.UsePrevious;
        replacementEvent.CustomColor = originalEvent.CustomColor;
        replacementEvent.StrobeColor = originalEvent.StrobeColor;
        replacementEvent.CustomLerpType = originalEvent.CustomLerpType;
        replacementEvent.CustomData = originalCustomData;
        replacementEvent.WriteCustom();
    }

    private void SetBoxIndex(int newIndex)
    {
        if (groupContext == null)
            return;
        displayedBoxIndex = Math.Clamp(
            newIndex,
            glsEventGridProvider.DisplayedLaneCount == 0 ? -1 : 0,
            glsEventGridProvider.DisplayedLaneCount - 1);
        boxIndex = glsEventGridProvider.GetAuthoredBoxIndex(displayedBoxIndex);
        glsEventGridProvider.TryGetDisplayedBox(displayedBoxIndex, out boxContext);
        deleteEventBoxButton.Selectable.interactable = boxIndex >= 0;
        moveUpEventBoxButton.Selectable.interactable = boxIndex > 0;
        moveDownEventBoxButton.Selectable.interactable = boxIndex >= 0 && boxIndex < groupContext.ReadOnlyBoxes.Count - 1;
        RefreshID();
        HandleEventBoxChanged(groupContext, boxContext);
    }

    private void RefreshID()
    {
        var count = glsEventGridProvider.DisplayedLaneCount;

        int i;
        for (i = 0; i < count; i++)
        {
            ToggleComponent idTab;
            if (i >= instantiatedIdTab.Count)
            {
                idTab = Instantiate(idTabPrefab, idTabTargetTransform);
                idTab.WithLabel((i + 1).ToString());
                idTab.OnValueChanged(HandleSetBoxIndex(i));
                instantiatedIdTab.Add(idTab);
            }
            else
                idTab = instantiatedIdTab[i];

            glsEventGridProvider.TryGetDisplayedBox(i, out var displayedBox);
            var labelColor = idTabPrefab.LabelColor;
            if (displayedBox.IsAutomaticAxisLane && displayedBox.ReadOnlyEvents.Count == 0)
            {
                labelColor.a *= 0.375f;
            }
            idTab.LabelColor = labelColor;
            idTab.SetValueWithoutNotify(i == displayedBoxIndex);
            idTab.Selectable.interactable = i != displayedBoxIndex;
            idTab.gameObject.SetActive(true);
        }

        for (; i < instantiatedIdTab.Count; i++) instantiatedIdTab[i].gameObject.SetActive(false);

        eventBoxIdText.SetValueWithoutNotify($"1  |  {count}");
    }

    private void HandleEventBoxChanged(BaseEventBoxGroup group, BaseEventBox box)
    {
        var groupSize = GetGroupSize(group);

        foreach (var t in instantiatedErrorText) Destroy(t);
        instantiatedErrorText.Clear();

        int i;
        for (i = 0; i < groupSize; i++)
        {
            Image idImage;
            if (i >= instantiatedIdImage.Count)
            {
                idImage = Instantiate(idImagePrefab, idImageTargetTransform);
                instantiatedIdImage.Add(idImage);
            }
            else
                idImage = instantiatedIdImage[i];

            idImage.color = new Color(0.1f, 0.1f, 0.1f);
            idImage.gameObject.SetActive(true);
        }

        for (; i < instantiatedIdImage.Count; i++) instantiatedIdImage[i].gameObject.SetActive(false);

        HashSet<int> affectedId = new();
        var currentBoxPassed = false;
        for (var x = 0; x < glsEventGridProvider.DisplayedLaneCount; x++)
        {
            glsEventGridProvider.TryGetDisplayedBox(x, out var b);
            if (b.GetAxis() != box.GetAxis())
            {
                continue;
            }
            var ifh = IndexFilterHelper.Convert(b.IndexFilter, groupSize);
            if (ifh == null)
            {
                if (instantiatedErrorText.Count > 10) continue;
                var t = Instantiate(errorTextPrefab, errorTextTargetTransform);
                t.text = $"[{x + 1}] Filter is invalid";
                t.gameObject.SetActive(true);
                instantiatedErrorText.Add(t);
                continue;
            }

            if (b == box) currentBoxPassed = true;
            foreach (var (element, _, _) in ifh)
            {
                if (0 > element || element >= groupSize)
                {
                    if (instantiatedErrorText.Count > 10) continue;
                    var t = Instantiate(errorTextPrefab, errorTextTargetTransform);
                    t.text = $"[{x + 1}] Filter returned OOB ID {element}";
                    t.gameObject.SetActive(true);
                    instantiatedErrorText.Add(t);
                    continue;
                }

                if (affectedId.Add(element))
                {
                    instantiatedIdImage[element].color =
                        b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
                }
                else if (b == box) instantiatedIdImage[element].color = Color.red;
            }
        }

        if (box == null)
        {
            inputContainer.SetActive(false);
            colorDistributionsView.gameObject.SetActive(false);
            strobeColorDistributionsView.gameObject.SetActive(false);
            return;
        }

        inputContainer.SetActive(true);

        var locIfh = IndexFilterHelper.Convert(box.IndexFilter, groupSize);
        filteredIdText.SetValueWithoutNotify(
            locIfh != null
                ? $"{groupSize}  |  {locIfh.Count}  |  {locIfh.VisibleCount}"
                : $"{groupSize}  |  0  |  0");

        beatDistributionWaveToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Wave);
        beatDistributionStepToggle.SetValueWithoutNotify(box.BeatDistributionType == (int)DistributionType.Step);
        beatDistributionInput.SetValueWithoutNotify(box.BeatDistribution);

        filterTypeSectionToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.Division);
        filterTypeStepToggle.SetValueWithoutNotify(box.IndexFilter.Type == (int)IndexFilterType.StepAndOffset);
        chunkInput.SetValueWithoutNotify(box.IndexFilter.Chunks);

        reverseToggle.SetValueWithoutNotify(box.IndexFilter.Reverse == 1);
        splitSectionsButton.gameObject.SetActive(box.IndexFilter.Type == (int)IndexFilterType.Division);
        splitSectionsButton.Selectable.interactable = GLSEventBoxCommand.CanSplitSections(group, box);
        if (box.IndexFilter.Type == (int)IndexFilterType.Division)
        {
            p0Input.MinValue = 1;
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0);
            p0Input.SetLabelText("Mapper", "eventbox.filter.sections.section");
            p1Input.MinValue = 1;
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1 + 1);
            p1Input.SetLabelText("Mapper", "eventbox.filter.sections.id");
        }
        else
        {
            p0Input.MinValue = 1;
            p0Input.SetValueWithoutNotify(box.IndexFilter.Param0 + 1);
            p0Input.SetLabelText("Mapper", "eventbox.filter.step.id");
            p1Input.MinValue = 0;
            p1Input.SetValueWithoutNotify(box.IndexFilter.Param1);
            p1Input.SetLabelText("Mapper", "eventbox.filter.step.step");
        }

        randomToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.RandomElements) > 0);
        inOrderToggle.SetValueWithoutNotify((box.IndexFilter.Random & (int)RandomType.KeepOrder) > 0);
        seedInput.SetValueWithoutNotify(box.IndexFilter.Seed);

        limitInput.SetValueWithoutNotify(box.IndexFilter.Limit * 100f);
        limitDurationToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Duration) > 0);
        limitDistributionToggle.SetValueWithoutNotify(
            (box.IndexFilter.LimitAffectsType & (int)LimitAlsoAffectType.Distribution) > 0);

        easeTypeDropdown.SetValueWithoutNotify(box.Easing);
        var isColorBox = box is BaseLightColorEventBox;
        colorDistributionsView.gameObject.SetActive(isColorBox);
        strobeColorDistributionsView.gameObject.SetActive(isColorBox);

        var td = beatmapRuntimeContext.TrackDefinitions.GetGlsOrDefault(groupContext.ID);
        // Axis visibility, values, and track availability are identical for every transform box.
        if (box is BaseLightTransformEventBox transformBox
            && groupContext is ILightTransformEventBoxGroup transformGroup)
        {
            var enabledAxes = transformGroup.GetEnabledAxes(td);
            axisObject.SetActive(true);
            axisXToggle.SetValueWithoutNotify(transformBox.Axis == (int)Axis.X);
            axisYToggle.SetValueWithoutNotify(transformBox.Axis == (int)Axis.Y);
            axisZToggle.SetValueWithoutNotify(transformBox.Axis == (int)Axis.Z);
            axisXToggle.gameObject.SetActive(enabledAxes[0]);
            axisYToggle.gameObject.SetActive(enabledAxes[1]);
            axisZToggle.gameObject.SetActive(enabledAxes[2]);
            axisXToggle.Selectable.interactable = enabledAxes[0];
            axisYToggle.Selectable.interactable = enabledAxes[1];
            axisZToggle.Selectable.interactable = enabledAxes[2];
        }

        switch (box)
        {
            case BaseLightColorEventBox lceb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lceb.BrightnessDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
                    .SetValueWithoutNotify(
                        lceb.BrightnessDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(lceb.BrightnessAffectFirst == 1);
                colorDistributionsView.SetColorDistributions(lceb.ColorDistributions);
                strobeColorDistributionsView.SetColorDistributions(lceb.StrobeColorDistributions);
                break;
            case BaseLightTransformEventBox currentTransformBox:
                // Rotation and Translation
                valueDistributionStepToggle.SetValueWithoutNotify(
                    currentTransformBox.ValueDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    currentTransformBox.ValueDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(currentTransformBox is BaseLightRotationEventBox
                        ? scrollPrecisionController.GetCurrentRotationPrecision
                        : scrollPrecisionController.GetCurrentTranslationPrecision)
                    .SetValueWithoutNotify(
                        currentTransformBox.ValueDistribution * currentTransformBox.ValueDistributionDisplayScale);
                affectFirstToggle.SetValueWithoutNotify(currentTransformBox.AffectFirst == 1);
                break;
            case BaseVfxEventEventBox ffeb:
                axisObject.SetActive(false);
                valueDistributionStepToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    ffeb.VfxDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentFloatFXPrecision)
                    .SetValueWithoutNotify(
                        ffeb.VfxDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(ffeb.VfxAffectFirst == 1);
                break;
            default:
                axisObject.SetActive(false);
                break;
        }
    }

    private int GetBoxEditIndex()
    {
        if (boxIndex >= 0)
        {
            return boxIndex;
        }

        // Materialization sorts the missing axis between the saved lanes already displayed before it.
        var index = 0;
        for (var lane = 0; lane < displayedBoxIndex; lane++)
        {
            if (glsEventGridProvider.GetAuthoredBoxIndex(lane) >= 0)
            {
                index++;
            }
        }
        return index;
    }

    private void ApplyBoxOption<T>(
        T value,
        Func<T, BaseEventBoxGroup, int, BaseEventBox, BaseEventBoxGroup> command)
    {
        var editedIndex = GetBoxEditIndex();
        var originalGroup = groupContext;
        var editedGroup = command(value, originalGroup, boxIndex, boxContext);
        if (editedGroup != null && editedGroup != originalGroup)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(editedIndex));
        }
    }

    private void MaterializeSelectedLane()
    {
        var editedIndex = GetBoxEditIndex();
        if (GLSEventBoxCommand.MaterializeAutomaticLane(groupContext, boxContext) != null)
        {
            SetBoxIndex(glsEventGridProvider.GetDisplayedLaneIndex(editedIndex));
        }
    }

    private void HandleBeatDistributionWaveValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)DistributionType.Wave, GLSEventBoxCommand.SetBeatDistributionType);
    }

    private void HandleBeatDistributionStepValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)DistributionType.Step, GLSEventBoxCommand.SetBeatDistributionType);
    }

    private void HandleBeatDistributionValueChanged(float value) =>
        ApplyBoxOption(value, GLSEventBoxCommand.SetBeatDistribution);

    private void HandleFilterTypeSectionValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)IndexFilterType.Division, GLSEventBoxCommand.SetType);
    }

    private void HandleFilterTypeStepValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)IndexFilterType.StepAndOffset, GLSEventBoxCommand.SetType);
    }

    private void HandleChunkValueChanged(int value) => ApplyBoxOption(value, GLSEventBoxCommand.SetChunk);

    private void HandleReverseValueChanged(bool value) =>
        ApplyBoxOption(value ? 1 : 0, GLSEventBoxCommand.SetReverse);

    private void HandleSplitSections()
    {
        if (groupContext == null || boxIndex < 0)
        {
            return;
        }
        GLSEventBoxCommand.SplitSections(groupContext, boxIndex);
    }

    private void HandleParam0ValueChanged(int value)
    {
        ApplyBoxOption(value, GLSEventBoxCommand.SetParam0);
        splitSectionsButton.Selectable.interactable = GLSEventBoxCommand.CanSplitSections(groupContext, boxContext);
    }

    private void HandleParam0EditingValueChanged(bool valid, int value)
    {
        splitSectionsButton.Selectable.interactable = valid
            && GLSEventBoxCommand.CanSplitSections(groupContext, boxContext, value);
    }

    private void HandleParam1ValueChanged(int value) => ApplyBoxOption(value, GLSEventBoxCommand.SetParam1);

    private void HandleRandomValueChanged(bool value) =>
        ApplyBoxOption(boxContext.IndexFilter.Random ^ (int)RandomType.RandomElements, GLSEventBoxCommand.SetRandom);

    private void HandleInOrderValueChanged(bool value) =>
        ApplyBoxOption(boxContext.IndexFilter.Random ^ (int)RandomType.KeepOrder, GLSEventBoxCommand.SetRandom);

    private void HandleSeedValueChanged(int value) => ApplyBoxOption(value, GLSEventBoxCommand.SetSeed);

    private void HandleRandomizeSeed() =>
        ApplyBoxOption(UnityEngine.Random.Range(int.MinValue, int.MaxValue), GLSEventBoxCommand.SetSeed);

    private void HandleResetSeed() => ApplyBoxOption(0, GLSEventBoxCommand.SetSeed);

    private void HandleAxisXValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)Axis.X, GLSEventBoxCommand.SetAxis);
    }

    private void HandleAxisYValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)Axis.Y, GLSEventBoxCommand.SetAxis);
    }

    private void HandleAxisZValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)Axis.Z, GLSEventBoxCommand.SetAxis);
    }

    private void HandleFlipValueChanged(bool value) =>
        ApplyBoxOption(value ? 1 : 0, GLSEventBoxCommand.SetFlip);

    private void HandleLimitValueChanged(float value) =>
        ApplyBoxOption(value / 100f, GLSEventBoxCommand.SetLimit);

    private void HandleLimitDurationValueChanged(bool value) =>
        ApplyBoxOption(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Duration,
            GLSEventBoxCommand.SetLimitAffectsType);

    private void HandleLimitDistributionValueChanged(bool value) =>
        ApplyBoxOption(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Distribution,
            GLSEventBoxCommand.SetLimitAffectsType);

    private void HandleValueDistributionWaveValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)DistributionType.Wave, GLSEventBoxCommand.SetValueDistributionType);
    }

    private void HandleValueDistributionStepValueChanged(bool value)
    {
        if (value)
            ApplyBoxOption((int)DistributionType.Step, GLSEventBoxCommand.SetValueDistributionType);
    }

    private void HandleValueDistributionValueChanged(float value) =>
        ApplyBoxOption(value, GLSEventBoxCommand.SetValueDistribution);

    private void HandleAffectFirstValueChanged(bool value) =>
        ApplyBoxOption(value ? 1 : 0, GLSEventBoxCommand.SetAffectFirst);

    private void HandleEaseTypeValueChanged(int value) => ApplyBoxOption(value, GLSEventBoxCommand.SetEasing);

    // Keep the local tooltip draft self-contained until its text is moved into localized string tables.
    private static void AddTooltip(ButtonComponent button, string text, string advancedText = null,
        string hotkeyActionMap = null, string hotkeyActionName = null)
    {
        var tooltip = button.gameObject.AddComponent<Tooltip>();
        tooltip.TooltipOverride = text;
        tooltip.AdvancedTooltip = advancedText ?? text;
        tooltip.AppearDelay = 0.25f;
        if (hotkeyActionMap != null) tooltip.HotkeyActionMap = hotkeyActionMap;
        if (hotkeyActionName != null) tooltip.HotkeyActionName = hotkeyActionName;
    }

    private int GetGroupSize(BaseEventBoxGroup group)
    {
        return group switch
        {
            BaseLightColorEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightColorGroupEffectManager
                .IdToEffect
                .TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightRotationEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightRotationGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseLightTranslationEventBoxGroup => beatmapRuntimeContext.Descriptor
                .LightTranslationGroupEffectManager
                .IdToEffect.TryGetValue(group.ID, out var fx)
                ? fx.Count
                : 0,
            BaseVfxEventEventBoxGroup =>
                beatmapRuntimeContext.Descriptor.FloatFxGroupEffectManager.IdToEffect.TryGetValue(
                    group.ID,
                    out var fx)
                    ? fx.Count
                    : 0,
            _ => 0
        };
    }
}
