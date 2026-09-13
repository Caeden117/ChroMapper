using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class BombPlacement : BasePlacement<BaseNote, NoteContainer, NoteGridContainer>
{
    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private ToggleColourDropdown dropdown;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a Bomb.");

    protected override BaseNote GenerateOriginalData() => new() { Type = (int)NoteType.Bomb };

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        // Initialize the queued bomb ghost from its current object-Chroma state instead of its prefab material.
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        PlacementVisualContainer.ModelController.MpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.ArrowMpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.NoteData = QueuedData;
        // if not set to null, the preview will stay chroma color after you turn chroma color off.
        PlacementVisualContainer.SetColor(QueuedData.CustomColor != null ? QueuedData.CustomColor : null);
        PlacementVisualContainer.UpdateMaterials();
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);

        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePositionRound(
                localPoint,
                precision,
                BeatmapConstant.PlayerYOffset / 2f);
            LanePosition.z = zPlacement;
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.PlayerYOffset / 2f);
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - (BeatmapConstant.PlayerYOffset / 2f))
                / BeatmapConstant.LaneSize;
            var snappedPosition = SnapWithHysteresis(rawX, rawY);

            LanePosition = new Vector3(
                snappedPosition.x + (gridViewController.IsOdd ? 0.5f : 0f),
                snappedPosition.y,
                zPlacement);
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(
                    LanePosition,
                    Bounds,
                    BeatmapConstant.PlayerYOffset / 2f);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // Bombs use the same object-Chroma setting as notes and walls, even after the picker flyout is closed as long as the Color Type picker at the top has Chroma Color selected (which defaults to off when coming to this view even if you had it on for lights).
        QueuedData.CustomColor = CanPlaceChromaObjects
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        if (PrecisionPlacementController.IsEnabled)
            QueuedData.CustomCoordinate = new Vector2(pos.x - 2f, pos.y);
        else
        {
            QueuedData.CustomCoordinate =
                !(Mathf.Approximately(vanillaX, pos.x)
                    && Mathf.Approximately(vanillaY, pos.y))
                    ? new Vector2(pos.x - 2f, pos.y)
                    : null;
        }

        // // Persist and repaint the queued bomb so its preview follows the current object-Chroma choice.
        QueuedData.WriteCustom();
        UpdateAppearance();
    }

    protected override void HandleRotationChanged(float rotation) => QueuedData.Rotation = (int)rotation;

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (dragged.Rotation != queued.Rotation)
        {
            dragged.Rotation = queued.Rotation;
            TracksManager.RefreshTracks();
        }
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        QueuedData.Rotation = (int)LaneRotationProvider.EditRotation;
    }
}
