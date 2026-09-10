using Beatmap.Containers;
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

        var original = BeatmapFactory.Clone(containerToEdit.ObjectData);

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue)
            * scrollPrecisionController.GetCurrentTimePrecision();

        containerToEdit.NJSData.RelativeNJS += modifier;
        if (containerToEdit.NJSData.RelativeNJS
            <= -BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed)
        {
            containerToEdit.NJSData.RelativeNJS =
                scrollPrecisionController.GetCurrentTimePrecision()
                - BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        }

        if (containerToEdit.NJSData.CompareTo(original) == 0) return;

        containerToEdit.UpdateNJSText();

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                containerToEdit.ObjectData,
                containerToEdit.ObjectData,
                original,
                "Modified NJS Event Value",
                mergeType: ActionMergeType.ModifyNJSEventValue));
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
        var index = System.Array.IndexOf(EasingValues, (EaseType)containerToEdit.NJSData.Easing);
        var next = EasingValues[
            ((index < 0 ? 0 : index) + direction + EasingValues.Length) % EasingValues.Length];
        VNJSEventCommand.SetEasing(containerToEdit, (int)next);
    }
}
