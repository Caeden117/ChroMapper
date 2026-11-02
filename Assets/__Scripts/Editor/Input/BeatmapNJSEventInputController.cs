using Beatmap.Containers;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapNJSEventInputController : BeatmapInputController<NJSEventContainer>, CMInput.INJSEventObjectsActions
{
    private static readonly EaseType[] EasingValues = (EaseType[])System.Enum.GetValues(typeof(EaseType));

    private static BeatmapNJSEventInputController ActiveInstance { get; set; }

    [SerializeField] private ScrollPrecisionController scrollPrecisionController;

    private void OnEnable() => ActiveInstance = this;

    private void OnDisable()
    {
        if (ActiveInstance == this)
            ActiveInstance = null;
    }

    public static bool IsCursorIntervalOwnedByPointer()
    {
        return ActiveInstance != null
            && ActiveInstance.RaycastFirstObject(out var container)
            && container != null
            && !container.Dragged;
    }

    public void OnTweakNJSValue(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        RaycastFirstObject(out var containerToEdit);
        if (containerToEdit == null) return;
        if (containerToEdit.NJSData.UsePrevious == 1) return;

        var precision = scrollPrecisionController.GetCurrentTimePrecision();
        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * precision;
        var noteJumpSpeed = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        void Tweak(BaseNJSEvent data)
        {
            if (data.UsePrevious == 1)
                return;

            data.RelativeNJS = CMMath.RoundToDecimals(data.RelativeNJS + modifier);
            if (data.RelativeNJS <= -noteJumpSpeed)
                data.RelativeNJS = CMMath.RoundToDecimals(precision - noteJumpSpeed);
        }

        if (HoverSelectionMutation.TryApply(containerToEdit.NJSData, Tweak, out _))
            return;

        var original = BeatmapFactory.Clone(containerToEdit.ObjectData);
        Tweak(containerToEdit.NJSData);

        if (containerToEdit.NJSData.HasSameContent(original))
            return;

        containerToEdit.UpdateNJSText();

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                containerToEdit.ObjectData,
                containerToEdit.ObjectData,
                original,
                "Modified NJS Event Value",
                mergeType: ActionMergeType.ModifyNJSEventValue,
                preserveSelection: true));
    }

    public void OnTweakVNJSEasing(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        RaycastFirstObject(out var containerToEdit);
        if (containerToEdit == null)
            return;

        var direction = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (direction == 0)
            return;
        int GetNextEasing(BaseNJSEvent data)
        {
            var index = System.Array.IndexOf(EasingValues, (EaseType)data.Easing);
            var currentIndex = index < 0
                ? 0
                : index;
            return (int)EasingValues[(currentIndex + direction + EasingValues.Length) % EasingValues.Length];
        }

        if (HoverSelectionMutation.TryApply(
                containerToEdit.NJSData,
                data => data.Easing = GetNextEasing(data),
                out _))
        {
            return;
        }

        VNJSEventCommand.SetEasing(containerToEdit, GetNextEasing(containerToEdit.NJSData));
    }
}
