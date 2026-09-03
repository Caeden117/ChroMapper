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

    private BaseEventBoxGroup groupContext;
    private BaseEventBox boxContext;
    private int boxIndex;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
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

        // Preserve the local tooltip draft while localization is decided separately from this review pass.
        AddTooltip(addEventBoxButton,
            "Add Box (+)",
            "Inserts a new empty event box after the currently selected one; each box independently controls which light IDs it targets and how values are distributed across them.");
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
            "Shifts the currently selected box one slot earlier in the list, which affects playback order when boxes share overlapping IDs and distributions.");
        AddTooltip(moveDownEventBoxButton,
            "Move Down",
            "Shifts the currently selected box one slot later in the list, which affects playback order when boxes share overlapping IDs and distributions.");

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
        if (!mode.HasFlag(EditingMode.EventBox)) SetBoxIndex(0);
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
            return;
        }

        boxIndex = group.ReadOnlyBoxes.Count > 0 ? Math.Clamp(boxIndex, 0, group.ReadOnlyBoxes.Count - 1) : -1;

        SetBoxIndex(boxIndex);
    }

    private void ConfigureAxisActions(BaseEventBoxGroup group)
    {
        // FloatFX and color boxes have no axis data, so never let axis-only operations rebuild their lane layout.
        var supportsAxes = group is BaseLightRotationEventBoxGroup || group is BaseLightTranslationEventBoxGroup;
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
        if (groupContext == null) return;
        GLSEventBoxCommand.AddEventBox(groupContext, ++boxIndex);
    }

    private void HandleAddIdsEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.AddAllIdsEventBox(groupContext, GetGroupSize(groupContext));
    }

    private void HandleAddAxesEventBox()
    {
        // Axis generation is meaningful only for transform GLS boxes; FloatFX must retain its ordinary filter lanes.
        if (groupContext is not (BaseLightRotationEventBoxGroup or BaseLightTranslationEventBoxGroup)) return;
        var td = beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(groupContext.ID);
        GLSEventBoxCommand.AddAllAxesEventBox(groupContext, td);
    }

    private void HandleAddIdsAndAxesEventBox()
    {
        // Axis generation is meaningful only for transform GLS boxes; FloatFX must retain its ordinary filter lanes.
        if (groupContext is not (BaseLightRotationEventBoxGroup or BaseLightTranslationEventBoxGroup)) return;
        var td = beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(groupContext.ID);
        GLSEventBoxCommand.AddAllAxesAndIdsEventBox(groupContext, td, GetGroupSize(groupContext));
    }

    private void HandleDeleteEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.DeleteEventBox(groupContext, boxIndex);
    }

    private void HandleDeletePruneEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.DeletePruneEventBox(groupContext);
    }

    private void HandleSortIdsEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.SortIdsEventBox(groupContext);
    }

    private void HandleSortAxesEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.SortAxesEventBox(groupContext);
    }

    private void HandleMoveDownEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.MoveDownEventBox(groupContext, boxIndex++);
    }

    private void HandleMoveUpEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.MoveUpEventBox(groupContext, boxIndex--);
    }

    private void HandleDuplicateEventBox()
    {
        if (groupContext == null) return;
        GLSEventBoxCommand.DuplicateEventBox(groupContext, boxIndex++);
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
        if (selectedGlsEvents.Count == 0) return;

        var byGroup = new Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>>();
        foreach (var selectedGlsEvent in selectedGlsEvents)
        {
            var group = selectedGlsEvent.EventBoxGroupData;
            if (group == null)
            {
                Debug.LogError("[PaintProperties] Cannot apply properties to a selected GLS node without an event box group.");
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
                        $"[PaintProperties] Cannot resolve selected {evt.GetType().Name}: groupId={oldGroup.ID}, " +
                        $"boxIndex={evt.BoxIndex}.");
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
                        break;
                    case BaseLightColorBase color when colorPlacement == null:
                        Debug.LogWarning("[EventBoxViewController] Color event but colorPlacement is null");
                        break;
                    case BaseLightRotationBase rotation when rotationPlacement != null:
                        // Log both source and queued rotation values to diagnose mismatched GLS rotation painting.
                        Debug.Log(
                            $"[PaintProperties] Rotation: groupId={oldGroup.ID}, boxIndex={evt.BoxIndex}, " +
                            $"eventIndex={location.EventIndex}, source={((BaseLightRotationBase)evt).Rotation}, " +
                            $"queued={rotationPlacement.QueuedData.Rotation}, loop={rotationPlacement.QueuedData.Loop}, " +
                            $"ease={rotationPlacement.QueuedData.EaseType}.");
                        // Rotation paint should update the editor-selected values without changing the node's existing spin direction choice.
                        rotation.Rotation = rotationPlacement.QueuedData.Rotation;
                        rotation.Loop = rotationPlacement.QueuedData.Loop;
                        rotation.EaseType = rotationPlacement.QueuedData.EaseType;
                        break;
                    case BaseLightRotationBase rotation when rotationPlacement == null:
                        Debug.LogWarning("[EventBoxViewController] Rotation event but rotationPlacement is null");
                        break;
                    case BaseLightTranslationBase translation when translationPlacement != null:
                        Debug.Log($"[EventBoxViewController] Applying translation properties");
                        translation.Translation = translationPlacement.QueuedData.Translation;
                        translation.EaseType = translationPlacement.QueuedData.EaseType;
                        break;
                    case BaseLightTranslationBase translation when translationPlacement == null:
                        Debug.LogWarning("[EventBoxViewController] Translation event but translationPlacement is null");
                        break;
                    case BaseFxEventFloat floatFx when floatFXPlacement != null:
                        Debug.Log($"[EventBoxViewController] Applying floatFX properties");
                        floatFx.Value = floatFXPlacement.QueuedData.Value;
                        floatFx.Easing = floatFXPlacement.QueuedData.Easing;
                        break;
                    case BaseFxEventFloat floatFx when floatFXPlacement == null:
                        Debug.LogWarning("[EventBoxViewController] FloatFX event but floatFXPlacement is null");
                        break;
                    default:
                        Debug.LogWarning($"[EventBoxViewController] Event type {newEvt.GetType().Name} not handled by apply logic");
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
        if (groupContext == null) return;
        boxIndex = Math.Clamp(
            newIndex,
            groupContext.ReadOnlyBoxes.Count == 0 ? -1 : 0,
            groupContext.ReadOnlyBoxes.Count - 1);
        boxContext = groupContext.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        RefreshID();
        HandleEventBoxChanged(groupContext, boxContext);
    }

    private void RefreshID()
    {
        var count = groupContext.ReadOnlyBoxes.Count;

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

            idTab.SetValueWithoutNotify(i == boxIndex);
            idTab.Selectable.interactable = i != boxIndex;
            idTab.gameObject.SetActive(true);
        }

        for (; i < instantiatedIdTab.Count; i++) instantiatedIdTab[i].gameObject.SetActive(false);

        eventBoxIdText.SetValueWithoutNotify($"1  |  {count}");
    }

    private void HandleEventBoxChanged(BaseEventBoxGroup group, BaseEventBox box)
    {
        var boxes = group.ReadOnlyBoxes;
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
        foreach (var (b, x) in boxes.AsValueEnumerable().Select((b, x) => (b, x)).Where(b => b.b.GetAxis() == box.GetAxis()))
        {
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

        var td = beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(groupContext.ID);
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
                break;
            case BaseLightRotationEventBox lreb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lreb.Axis == (int)Axis.Z);
                axisXToggle.Selectable.interactable = td.RotationTracks[0];
                axisYToggle.Selectable.interactable = td.RotationTracks[1];
                axisZToggle.Selectable.interactable = td.RotationTracks[2];
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lreb.RotationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentRotationPrecision)
                    .SetValueWithoutNotify(
                        lreb.RotationDistribution);
                affectFirstToggle.SetValueWithoutNotify(lreb.RotationAffectFirst == 1);
                break;
            case BaseLightTranslationEventBox lteb:
                axisObject.SetActive(true);
                axisXToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.X);
                axisYToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Y);
                axisZToggle.SetValueWithoutNotify(lteb.Axis == (int)Axis.Z);
                axisXToggle.Selectable.interactable = td.TranslationTracks[0];
                axisYToggle.Selectable.interactable = td.TranslationTracks[1];
                axisZToggle.Selectable.interactable = td.TranslationTracks[2];
                valueDistributionStepToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Step);
                valueDistributionWaveToggle.SetValueWithoutNotify(
                    lteb.TranslationDistributionType == (int)DistributionType.Wave);
                valueDistributionInput
                    .WithScrollPrecision(scrollPrecisionController.GetCurrentTranslationPrecision)
                    .SetValueWithoutNotify(
                        lteb.TranslationDistribution * 100f);
                affectFirstToggle.SetValueWithoutNotify(lteb.TranslationAffectFirst == 1);
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

    private void HandleBeatDistributionWaveValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetBeatDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleBeatDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetBeatDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleBeatDistributionValueChanged(float value) =>
        GLSEventBoxCommand.SetBeatDistribution(value, groupContext, boxIndex);

    private void HandleFilterTypeSectionValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetType((int)IndexFilterType.Division, groupContext, boxIndex);
    }

    private void HandleFilterTypeStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetType((int)IndexFilterType.StepAndOffset, groupContext, boxIndex);
    }

    private void HandleChunkValueChanged(int value) => GLSEventBoxCommand.SetChunk(value, groupContext, boxIndex);

    private void HandleReverseValueChanged(bool value) =>
        GLSEventBoxCommand.SetReverse(value ? 1 : 0, groupContext, boxIndex);

    private void HandleParam0ValueChanged(int value) => GLSEventBoxCommand.SetParam0(value, groupContext, boxIndex);

    private void HandleParam1ValueChanged(int value) => GLSEventBoxCommand.SetParam1(value, groupContext, boxIndex);

    private void HandleRandomValueChanged(bool value) =>
        GLSEventBoxCommand.SetRandom(
            boxContext.IndexFilter.Random ^ (int)RandomType.RandomElements,
            groupContext,
            boxIndex);

    private void HandleInOrderValueChanged(bool value) =>
        GLSEventBoxCommand.SetRandom(boxContext.IndexFilter.Random ^ (int)RandomType.KeepOrder, groupContext, boxIndex);

    private void HandleSeedValueChanged(int value) => GLSEventBoxCommand.SetSeed(value, groupContext, boxIndex);

    private void HandleRandomizeSeed() =>
        GLSEventBoxCommand.SetSeed(UnityEngine.Random.Range(int.MinValue, int.MaxValue), groupContext, boxIndex);

    private void HandleResetSeed() => GLSEventBoxCommand.SetSeed(0, groupContext, boxIndex);

    private void HandleAxisXValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.X, groupContext, boxIndex);
    }

    private void HandleAxisYValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.Y, groupContext, boxIndex);
    }

    private void HandleAxisZValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetAxis((int)Axis.Z, groupContext, boxIndex);
    }

    private void HandleFlipValueChanged(bool value) =>
        GLSEventBoxCommand.SetFlip(value ? 1 : 0, groupContext, boxIndex);

    private void HandleLimitValueChanged(float value) =>
        GLSEventBoxCommand.SetLimit(value / 100f, groupContext, boxIndex);

    private void HandleLimitDurationValueChanged(bool value) =>
        GLSEventBoxCommand.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Duration,
            groupContext,
            boxIndex);

    private void HandleLimitDistributionValueChanged(bool value) =>
        GLSEventBoxCommand.SetLimitAffectsType(
            boxContext.IndexFilter.LimitAffectsType ^ (int)LimitAlsoAffectType.Distribution,
            groupContext,
            boxIndex);

    private void HandleValueDistributionWaveValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetValueDistributionType((int)DistributionType.Wave, groupContext, boxIndex);
    }

    private void HandleValueDistributionStepValueChanged(bool value)
    {
        if (value) GLSEventBoxCommand.SetValueDistributionType((int)DistributionType.Step, groupContext, boxIndex);
    }

    private void HandleValueDistributionValueChanged(float value) =>
        GLSEventBoxCommand.SetValueDistribution(value, groupContext, boxIndex);

    private void HandleAffectFirstValueChanged(bool value) =>
        GLSEventBoxCommand.SetAffectFirst(value ? 1 : 0, groupContext, boxIndex);

    private void HandleEaseTypeValueChanged(int value) => GLSEventBoxCommand.SetEasing(value, groupContext, boxIndex);

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
