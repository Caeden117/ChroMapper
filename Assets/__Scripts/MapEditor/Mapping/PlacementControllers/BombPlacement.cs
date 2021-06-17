using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using System;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
{
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;

    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;
    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
            {
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            }
            return false;
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_BOMB, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData._customData != null && queuedData._customData.HasKey("_color"))
            {
                queuedData._customData.Remove("_color");

                if (queuedData._customData.Count <= 0) //Set customData to null if there is no customData to store
                {
                    queuedData._customData = null;
                }
            }
        }

        if (usePrecisionPlacement)
        {
            queuedData._lineIndex = queuedData._lineLayer = 0;

            instantiatedContainer.transform.localPosition = roundedHit;

            if (queuedData._customData == null) queuedData._customData = new JSONObject();

            JSONArray position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            queuedData._customData["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            if (queuedData._customData != null && queuedData._customData.HasKey("_position"))
            {
                queuedData._customData.Remove("_position"); //Remove NE position since we are no longer working with it.

                if (queuedData._customData.Count <= 0) //Set customData to null if there is no customData to store
                {
                    queuedData._customData = null;
                }
            }

            precisionPlacement.TogglePrecisionPlacement(false);
            queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        instantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        instantiatedContainer.UpdateMaterials();
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
    }
}
