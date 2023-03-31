using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
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

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");

    public override BeatmapNote GenerateOriginalData() 
    {
        if (Settings.Instance.Load_MapV3)
            return new BeatmapBombNote(0, 0, 0);
        else
            return new BeatmapNote(0, 0, 0, BeatmapNote.NoteTypeBomb, BeatmapNote.NoteCutDirectionDown);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.GetOrCreateCustomData()[MapLoader.heckUnderscore + "color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData.CustomData != null && queuedData.CustomData.HasKey(MapLoader.heckUnderscore + "color"))
            {
                queuedData.CustomData.Remove(MapLoader.heckUnderscore + "color");

                if (queuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    queuedData.CustomData = null;
            }
        }

        if (UsePrecisionPlacement)
        {
            queuedData.LineIndex = queuedData.LineLayer = 0;

            instantiatedContainer.transform.localPosition = roundedHit;

            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            queuedData.CustomData[MapLoader.heckPosition] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            if (queuedData.CustomData != null && queuedData.CustomData.HasKey(MapLoader.heckPosition))
            {
                queuedData.CustomData.Remove(MapLoader.heckPosition); //Remove NE position since we are no longer working with it.

                if (queuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    queuedData.CustomData = null;
            }

            precisionPlacement.TogglePrecisionPlacement(false);
            queuedData.LineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData.LineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        instantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        instantiatedContainer.UpdateMaterials();
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged.Time = queued.Time;
        dragged.LineIndex = queued.LineIndex;
        dragged.LineLayer = queued.LineLayer;
    }
}
