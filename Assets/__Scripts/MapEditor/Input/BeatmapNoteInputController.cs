using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapNoteInputController : BeatmapInputController<BeatmapNoteContainer>, CMInput.INoteObjectsActions
{
    [FormerlySerializedAs("noteAppearanceSO")] [SerializeField] private NoteAppearanceSO noteAppearanceSo;
    public bool QuickModificationActive;

    private readonly Dictionary<int, int> cutDirectionMovedBackward = new Dictionary<int, int>
    {
        {BeatmapNote.NoteCutDirectionAny, BeatmapNote.NoteCutDirectionAny},
        {BeatmapNote.NoteCutDirectionDownLeft, BeatmapNote.NoteCutDirectionDown},
        {BeatmapNote.NoteCutDirectionLeft, BeatmapNote.NoteCutDirectionDownLeft},
        {BeatmapNote.NoteCutDirectionUpLeft, BeatmapNote.NoteCutDirectionLeft},
        {BeatmapNote.NoteCutDirectionUp, BeatmapNote.NoteCutDirectionUpLeft},
        {BeatmapNote.NoteCutDirectionUpRight, BeatmapNote.NoteCutDirectionUp},
        {BeatmapNote.NoteCutDirectionRight, BeatmapNote.NoteCutDirectionUpRight},
        {BeatmapNote.NoteCutDirectionDownRight, BeatmapNote.NoteCutDirectionRight},
        {BeatmapNote.NoteCutDirectionDown, BeatmapNote.NoteCutDirectionDownRight},
        {BeatmapNote.NoteCutDirectionNone, BeatmapNote.NoteCutDirectionNone}
    };

    private readonly Dictionary<int, int> cutDirectionMovedForward = new Dictionary<int, int>
    {
        {BeatmapNote.NoteCutDirectionAny, BeatmapNote.NoteCutDirectionAny},
        {BeatmapNote.NoteCutDirectionDown, BeatmapNote.NoteCutDirectionDownLeft},
        {BeatmapNote.NoteCutDirectionDownLeft, BeatmapNote.NoteCutDirectionLeft},
        {BeatmapNote.NoteCutDirectionLeft, BeatmapNote.NoteCutDirectionUpLeft},
        {BeatmapNote.NoteCutDirectionUpLeft, BeatmapNote.NoteCutDirectionUp},
        {BeatmapNote.NoteCutDirectionUp, BeatmapNote.NoteCutDirectionUpRight},
        {BeatmapNote.NoteCutDirectionUpRight, BeatmapNote.NoteCutDirectionRight},
        {BeatmapNote.NoteCutDirectionRight, BeatmapNote.NoteCutDirectionDownRight},
        {BeatmapNote.NoteCutDirectionDownRight, BeatmapNote.NoteCutDirectionDown},
        {BeatmapNote.NoteCutDirectionNone, BeatmapNote.NoteCutDirectionNone}
    };

    //Do some shit later lmao
    public void OnInvertNoteColors(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            !KeybindsController.IsMouseInWindow || !context.performed)
        {
            return;
        }

        RaycastFirstObject(out var note);
        if (note != null && !note.Dragging) InvertNote(note);
    }

    public void OnQuickDirectionModifier(InputAction.CallbackContext context) =>
        QuickModificationActive = context.performed;

    public void OnUpdateNoteDirection(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        if (!context.performed) return;

        var shiftForward = context.ReadValue<float>() > 0;
        RaycastFirstObject(out var note);
        if (note != null) UpdateNoteDirection(note, shiftForward);
    }

    public void InvertNote(BeatmapNoteContainer note)
    {
        if (note.MapNoteData.Type == BeatmapNote.NoteTypeBomb) return;

        var original = BeatmapObject.GenerateCopy(note.ObjectData);
        var newType = note.MapNoteData.Type == BeatmapNote.NoteTypeA
            ? BeatmapNote.NoteTypeB
            : BeatmapNote.NoteTypeA;
        note.MapNoteData.Type = newType;
        noteAppearanceSo.SetNoteAppearance(note);
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<NotesContainer>(BeatmapObject.ObjectType.Note);
        collection.RefreshSpecialAngles(note.ObjectData, false, false);
        collection.RefreshSpecialAngles(original, false, false);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original));
    }

    public void UpdateNoteDirection(BeatmapNoteContainer note, bool shiftForward)
    {
        var original = BeatmapObject.GenerateCopy(note.ObjectData);
        note.MapNoteData.CutDirection =
            (shiftForward ? cutDirectionMovedForward : cutDirectionMovedBackward)[note.MapNoteData.CutDirection];
        note.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(note.MapNoteData);
        BeatmapObjectContainerCollection.GetCollectionForType<NotesContainer>(BeatmapObject.ObjectType.Note)
            .RefreshSpecialAngles(note.ObjectData, false, false);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original));
    }
}
