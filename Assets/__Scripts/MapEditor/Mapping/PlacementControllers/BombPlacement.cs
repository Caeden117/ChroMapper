using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

public class BombPlacement : PlacementController<BaseNote, NoteContainer, NoteGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private ToggleColourDropdown dropdown;

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            return false;
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");

    public override BaseNote GenerateOriginalData() => BeatmapFactory.Bomb(0, 0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, SongBpmTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        queuedData.CustomColor = (CanPlaceChromaObjects && dropdown.Visible)
            ? (Color?)colorPicker.CurrentColor
            : null;

        var posX = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
        var posY = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 1.5f);

        var vanillaX = Mathf.Clamp(posX, 0, 3);
        var vanillaY = Mathf.Clamp(posY, 0, 2);

        var vanillaBounds = vanillaX == posX && vanillaY == posY;

        queuedData.PosX = vanillaX;
        queuedData.PosY = vanillaY;

        if (UsePrecisionPlacement)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit.x = Mathf.Round(roundedHit.x * precision) / precision;
            roundedHit.y = Mathf.Round(roundedHit.y * precision) / precision;
            instantiatedContainer.transform.localPosition = roundedHit;

            queuedData.CustomCoordinate = new Vector2(roundedHit.x - 0.5f, roundedHit.y - 1.5f);

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            queuedData.CustomCoordinate = !vanillaBounds
                ? (JSONNode)new Vector2(Mathf.Round(roundedHit.x - 0.5f), Mathf.Round(roundedHit.y - 1.5f))
                : null;
        }

        instantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        instantiatedContainer.UpdateMaterials();
    }

    public override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }
}
