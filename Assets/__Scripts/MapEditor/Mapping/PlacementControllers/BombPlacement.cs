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

    public override BeatmapNote GenerateOriginalData() =>
        new BeatmapNote(0, 0, 0, BeatmapNote.NoteTypeBomb, BeatmapNote.NoteCutDirectionDown);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            QueuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (QueuedData.CustomData != null && QueuedData.CustomData.HasKey("_color"))
            {
                QueuedData.CustomData.Remove("_color");

                if (QueuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    QueuedData.CustomData = null;
            }
        }

        if (UsePrecisionPlacement)
        {
            QueuedData.LineIndex = QueuedData.LineLayer = 0;

            InstantiatedContainer.transform.localPosition = roundedHit;

            if (QueuedData.CustomData == null) QueuedData.CustomData = new JSONObject();

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            QueuedData.CustomData["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            if (QueuedData.CustomData != null && QueuedData.CustomData.HasKey("_position"))
            {
                QueuedData.CustomData.Remove("_position"); //Remove NE position since we are no longer working with it.

                if (QueuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    QueuedData.CustomData = null;
            }

            precisionPlacement.TogglePrecisionPlacement(false);
            QueuedData.LineIndex = Mathf.RoundToInt(InstantiatedContainer.transform.localPosition.x + 1.5f);
            QueuedData.LineLayer = Mathf.RoundToInt(InstantiatedContainer.transform.localPosition.y - 0.5f);
        }

        InstantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        InstantiatedContainer.UpdateMaterials();
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged.Time = queued.Time;
        dragged.LineIndex = queued.LineIndex;
        dragged.LineLayer = queued.LineLayer;
    }
}
