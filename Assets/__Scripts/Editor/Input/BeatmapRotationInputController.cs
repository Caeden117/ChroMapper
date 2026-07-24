using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapRotationInputController : BeatmapInputController<ObjectContainer>,
                                              CMInput.IRotationObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] private GridLane gridLane;

    public event Action<float> OnRotationInput;

    protected override bool SpecialCaseContainer(ObjectContainer con) => con is RotationEventContainer;

    public void OnRotateClockwiseHover(InputAction.CallbackContext context) =>
        HandleRotateDirectionalHover(context, true);

    public void OnRotateClockwiseGrid(InputAction.CallbackContext context) =>
        HandleRotateDirectionalGrid(context, true);

    public void OnRotateCounterClockwiseHover(InputAction.CallbackContext context) =>
        HandleRotateDirectionalHover(context, false);

    public void OnRotateCounterClockwiseGrid(InputAction.CallbackContext context) =>
        HandleRotateDirectionalGrid(context, false);

    public void HandleRotateDirectionalHover(InputAction.CallbackContext context, bool clockwise)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con))
            return;
        var prec = scrollPrecisionController.GetCurrentAngleOffsetPrecision();

        switch (con)
        {
            case RotationEventContainer:
                RotationCommand.RotateObject(con.ObjectData, clockwise, prec);
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotationCommand.RotateObject(con.ObjectData, clockwise, prec);
                break;
        }
    }

    public void HandleRotateDirectionalGrid(InputAction.CallbackContext context, bool clockwise)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed)
            return;
        var prec = scrollPrecisionController.GetCurrentAngleOffsetPrecision();
        var modifier = clockwise ? 1 : -1;

        if (EditContext.EditingMode.HasFlag(editMode) && !gridLane.Hide)
            RotationCommand.PlaceEventInPlace(atsc.CurrentJsonTime, clockwise, prec);
        else
        {
            laneRotationProvider.SetEditRotation(
                Mathf.RoundToInt(
                    Mathf.Round((laneRotationProvider.EditRotation + (modifier * prec)) * 1_000f) / 1_000f));
        }
    }

    public void OnRotateCopyHover(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con))
            return;
        switch (con)
        {
            case RotationEventContainer evt:
                RotationCommand.SetRotation(
                    con.ObjectData,
                    Mathf.DeltaAngle(evt.EventData.Rotation, laneRotationProvider.EditRotation));
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotationCommand.SetRotation(con.ObjectData, laneRotationProvider.EditRotation);
                break;
        }
    }

    public void OnRotateCopyGrid(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con))
            return;
        int rotation;
        switch (con)
        {
            case RotationEventContainer evt:
                rotation = (int)evt.EventData.Rotation;
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                rotation = (con.ObjectData as BaseGrid)?.Rotation ?? 0;
                break;
            default:
                return;
        }

        laneRotationProvider.SetEditRotation(rotation);
    }

    public void OnModifyHover(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var prec = scrollPrecisionController.GetCurrentAngleOffsetPrecision();
        RotationCommand.ModifyHover(e.EventData, modifier, prec);
    }

    public void OnInvert(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsHoverKeyHeld
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        RotationCommand.Invert(e.EventData);
    }

    public void OnRotation15Degrees(InputAction.CallbackContext context) => OnRotationNumberDegrees(context, 15);

    public void OnRotation30Degrees(InputAction.CallbackContext context) => OnRotationNumberDegrees(context, 30);

    public void OnRotation45Degrees(InputAction.CallbackContext context) => OnRotationNumberDegrees(context, 45);

    public void OnRotation60Degrees(InputAction.CallbackContext context) => OnRotationNumberDegrees(context, 60);

    private void OnRotationNumberDegrees(InputAction.CallbackContext context, int rotation)
    {
        if (!context.performed) return;

        if (KeybindsController.IsHoverKeyHeld)
            HandleRotationHoverInput(context, rotation);
        else
            OnRotationInput?.Invoke(rotation);
    }

    public void HandleRotationHoverInput(InputAction.CallbackContext context, float rotation)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        RotationCommand.SetRotationInfer(e.EventData, rotation);
    }
}
