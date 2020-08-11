using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>, CMInput.INotePlacementActions
{
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;

    // Chromatoggle Stuff
    public static readonly string ChromaToggleKey = "PlaceChromaToggleNote";
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    private bool upNote = false;
    private bool leftNote = false;
    private bool downNote = false;
    private bool rightNote = false;

    public override bool IsValid
    {
        get
        {
            if (Settings.Instance.PrecisionPlacementGrid)
            {
                return base.IsValid || (KeybindsController.ShiftHeld && IsActive && !NodeEditorController.IsActive);
            }
            else
            {
                return base.IsValid;
            }
        }
    }

    public static bool CanPlaceChromaToggleNotes
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaToggleKey))
            {
                return (bool)Settings.NonPersistentSettings[ChromaToggleKey];
            }
            return false;
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(BeatmapObject.GenerateCopy(spawned), container, "Placed a note.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_A, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public void PlaceChromaToggle(bool v)
    {
        if (Settings.NonPersistentSettings.ContainsKey(ChromaToggleKey))
        {
            Settings.NonPersistentSettings[ChromaToggleKey] = v;
        }
        else
        {
            Settings.NonPersistentSettings.Add(ChromaToggleKey, v);
        }
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if ChromaToggle notes button is active and apply _color
        if (CanPlaceChromaToggleNotes && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            JSONArray color = new JSONArray();
            if (queuedData._customData == null) queuedData._customData = new JSONObject();
            queuedData._customData["_color"] = colorPicker.CurrentColor;
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

        if (KeybindsController.ShiftHeld && Settings.Instance.PrecisionPlacementGrid)
        {
            queuedData._lineIndex = queuedData._lineLayer = 0;

            instantiatedContainer.transform.localPosition = roundedHit;

            if (queuedData._customData == null) queuedData._customData = new JSONObject();

            JSONArray position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            queuedData._customData["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            if (queuedData._customData != null && queuedData._customData.HasKey("_position"))
            {
                queuedData._customData.Remove("_position"); //Remove NE position since we are no longer working with it.

                if (queuedData._customData.Count <= 0) //Set customData to null if there is no customData to store
                {
                    queuedData._customData = null;
                }
            }
            queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        UpdateAppearance();
    }

    public void UpdateCut(int value)
    {
        queuedData._cutDirection = value;
        UpdateAppearance();
    }

    public void UpdateType(int type)
    {
        queuedData._type = type;
        UpdateAppearance();
    }

    public void ChangeChromaToggle(bool isChromaToggleNote)
    {
        if (isChromaToggleNote)
        {
            BeatmapChromaNote data = new BeatmapChromaNote(queuedData);
            data.BombRotation = BeatmapChromaNote.ALTERNATE;
            queuedData = data;
        }
        else if (queuedData is BeatmapChromaNote data) queuedData = data.ConvertToNote();
        UpdateAppearance();
    }

    public void UpdateChromaValue(int chromaNoteValue)
    {
        if (queuedData is BeatmapChromaNote chroma)
        {
            chroma.BombRotation = chromaNoteValue;
            UpdateAppearance();
        }
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) return;
        instantiatedContainer.mapNoteData = queuedData;
        noteAppearanceSO.SetNoteAppearance(instantiatedContainer);
        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.material.HasProperty("_AlwaysTranslucent") && renderer.material.GetFloat("_AlwaysTranslucent") == 1)
                continue; //Dont want to do this shit almost every frame.
            renderer.material.SetFloat("_AlwaysTranslucent", 1);
        }
        instantiatedContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
        dragged._cutDirection = queued._cutDirection;
        if (draggedObjectContainer != null)
        {
            draggedObjectContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(dragged);
        }
        noteAppearanceSO?.SetNoteAppearance(draggedObjectContainer);
    }

    //TODO perhaps make a helper function to deal with the context.performed and context.canceled checks
    public void OnDownNote(InputAction.CallbackContext context)
    {
        downNote = context.performed;
        if (context.performed) HandleDirectionValues();
    }

    public void OnLeftNote(InputAction.CallbackContext context)
    {
        leftNote = context.performed;
        if (context.performed) HandleDirectionValues();
    }

    public void OnUpNote(InputAction.CallbackContext context)
    {
        upNote = context.performed;
        if (context.performed) HandleDirectionValues();
    }

    public void OnRightNote(InputAction.CallbackContext context)
    {
        rightNote = context.performed;
        if (context.performed) HandleDirectionValues();
    }

    private void HandleDirectionValues()
    {
        deleteToolController.UpdateDeletion(false);

        bool handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        bool handleLeftRightNotes = leftNote ^ rightNote;

        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP);
            else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);
        }
        else if (handleUpDownNotes && handleLeftRightNotes) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
                else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
            }
            else
            {
                if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
                else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
            }
        }
    }

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(false);
        UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_ANY);
    }
}
