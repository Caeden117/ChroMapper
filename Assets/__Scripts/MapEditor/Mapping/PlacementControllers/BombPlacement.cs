using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;
using System;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
{
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;

    public override bool IsValid
    {
        get => Settings.Instance.PrecisionPlacementGrid ? base.IsValid || (KeybindsController.ShiftHeld && IsActive) : base.IsValid;
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_BOMB, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);
        if (KeybindsController.ShiftHeld && Settings.Instance.PrecisionPlacementGrid)
        {
            queuedData._lineIndex = queuedData._lineLayer = 0;

            instantiatedContainer.transform.localPosition = roundedHit;

            if (queuedData._customData == null) queuedData._customData = new JSONObject();

            JSONArray position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x, 3);
            position[1] = Math.Round(roundedHit.y, 3);
            queuedData._customData["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);
            queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.material.HasProperty("_AlwaysTranslucent") && renderer.material.GetFloat("_AlwaysTranslucent") == 1)
                continue; //Dont want to do this shit almost every frame.
            renderer.material.SetFloat("_AlwaysTranslucent", 1);
        }
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
    }
}
