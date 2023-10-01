using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapNoteInputController : BeatmapInputController<NoteContainer>, CMInput.INoteObjectsActions
{
    [FormerlySerializedAs("noteAppearanceSO")][SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private ArcAppearanceSO arcAppearanceSo;
    [SerializeField] private ChainAppearanceSO chainAppearanceSo;

    public bool QuickModificationActive;

    private readonly Dictionary<int, int> cutDirectionMovedBackward = new Dictionary<int, int>
    {
        {(int)NoteCutDirection.Any, (int)NoteCutDirection.Any},
        {(int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Down},
        {(int)NoteCutDirection.Left, (int)NoteCutDirection.DownLeft},
        {(int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Left},
        {(int)NoteCutDirection.Up, (int)NoteCutDirection.UpLeft},
        {(int)NoteCutDirection.UpRight, (int)NoteCutDirection.Up},
        {(int)NoteCutDirection.Right, (int)NoteCutDirection.UpRight},
        {(int)NoteCutDirection.DownRight, (int)NoteCutDirection.Right},
        {(int)NoteCutDirection.Down, (int)NoteCutDirection.DownRight},
        {(int)NoteCutDirection.None, (int)NoteCutDirection.None}
    };

    private readonly Dictionary<int, int> cutDirectionMovedForward = new Dictionary<int, int>
    {
        {(int)NoteCutDirection.Any, (int)NoteCutDirection.Any},
        {(int)NoteCutDirection.Down, (int)NoteCutDirection.DownLeft},
        {(int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Left},
        {(int)NoteCutDirection.Left, (int)NoteCutDirection.UpLeft},
        {(int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Up},
        {(int)NoteCutDirection.Up, (int)NoteCutDirection.UpRight},
        {(int)NoteCutDirection.UpRight, (int)NoteCutDirection.Right},
        {(int)NoteCutDirection.Right, (int)NoteCutDirection.DownRight},
        {(int)NoteCutDirection.DownRight, (int)NoteCutDirection.Down},
        {(int)NoteCutDirection.None, (int)NoteCutDirection.None}
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

    public void OnUpdateNotePreciseDirection(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        if (!context.performed) return;

        var shiftForward = context.ReadValue<float>() > 0;
        RaycastFirstObject(out var note);
        if (note != null) UpdateNotePreciseDirection(note, shiftForward);
    }

    public void InvertNote(NoteContainer note)
    {
        if (note.NoteData.Type == (int)NoteType.Bomb) return;

        var original = BeatmapFactory.Clone(note.ObjectData);
        var newType = note.NoteData.Type == (int)NoteType.Red
            ? (int)NoteType.Blue
            : (int)NoteType.Red;
        note.NoteData.Type = newType;
        noteAppearanceSo.SetNoteAppearance(note);
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        collection.RefreshSpecialAngles(note.ObjectData, false, false);
        collection.RefreshSpecialAngles(original, false, false);

        var actions = new List<BeatmapAction> { new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original) };
        InvertAttachedSliders(note, actions);

        BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true, "Note inversion"));
    }

    private void InvertAttachedSliders(NoteContainer note, ICollection<BeatmapAction> actions)
    {
        var noteData = note.NoteData;
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead || isConnectedToTail)
            {
                var arcOriginal = BeatmapFactory.Clone(arcData);
                arcData.Color = noteData.Color;
                arcAppearanceSo.SetArcAppearance(arcContainer.Value as ArcContainer);

                actions.Add(new BeatmapObjectModifiedAction(arcData, arcData, arcOriginal));
            }
        }

        var chainCollection = BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon && chainData.GetPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var chainOriginal = BeatmapFactory.Clone(chainData);
                chainData.Color = noteData.Color;
                chainAppearanceSo.SetChainAppearance(chainContainer.Value as ChainContainer);

                actions.Add(new BeatmapObjectModifiedAction(chainData, chainData, chainOriginal));
            }
        }
    }

    public void UpdateNoteDirection(NoteContainer note, bool shiftForward)
    {
        var original = BeatmapFactory.Clone(note.ObjectData);
        note.NoteData.CutDirection = ((shiftForward ^ Settings.Instance.InvertScrollNoteAngle)
            ? cutDirectionMovedBackward : cutDirectionMovedForward)[note.NoteData.CutDirection];
        note.transform.localEulerAngles = NoteContainer.Directionalize(note.NoteData);
        BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note)
            .RefreshSpecialAngles(note.ObjectData, false, false);

        var actions = new List<BeatmapAction> { new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original) };
        CommonNotePlacement.UpdateAttachedSlidersDirection(note.NoteData, actions);

        BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true, "Update Note Direction"));
    }

    public void UpdateNotePreciseDirection(NoteContainer note, bool shiftForward)
    {
        var original = BeatmapFactory.Clone(note.ObjectData);

        if (note.NoteData is V3ColorNote cnote)
        {
            cnote.AngleOffset += (shiftForward ^ Settings.Instance.InvertScrollNoteAngle)
                ? 5
                : -5;

            BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note)
                .RefreshSpecialAngles(note.ObjectData, false, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original));
        }
        else
        {
            // V2 note unsupported. Could implement either ME or NE for V2 note.
        }

    }
}
