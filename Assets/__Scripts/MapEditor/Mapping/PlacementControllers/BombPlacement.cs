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
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData.CustomData != null)
            {
                queuedData.CustomColor = null;
            }
        }

        if (UsePrecisionPlacement)
        {
            queuedData.PosX = queuedData.PosY = 0;

            var precision = Atsc.GridMeasureSnapping;
            roundedHit.x = Mathf.Round(roundedHit.x * precision) / precision;
            roundedHit.y = Mathf.Round(roundedHit.y * precision) / precision;
            instantiatedContainer.transform.localPosition = roundedHit;

            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            queuedData.CustomCoordinate = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            if (queuedData.CustomData != null)
            {
                queuedData.CustomCoordinate = null; //Remove NE position since we are no longer working with it.

                if (queuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    queuedData.CustomData = null;
            }

            precisionPlacement.TogglePrecisionPlacement(false);
            queuedData.PosX = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData.PosY = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        instantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        instantiatedContainer.UpdateMaterials();
    }

    public override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.Time = queued.Time;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }
}
