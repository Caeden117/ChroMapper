using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private ArcAppearanceSO arcAppearance;
    [SerializeField] private ChainAppearanceSO chainAppearance;
    
    private static readonly Dictionary<int, int> cutDirectionMovedBackward = new()
    {
        { (int)NoteCutDirection.Any, (int)NoteCutDirection.Any },
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Down },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Up, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.Up },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.Right },
        { (int)NoteCutDirection.Down, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.None, (int)NoteCutDirection.None }
    };

    private static readonly Dictionary<int, int> cutDirectionMovedForward = new()
    {
        { (int)NoteCutDirection.Any, (int)NoteCutDirection.Any },
        { (int)NoteCutDirection.Down, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Up },
        { (int)NoteCutDirection.Up, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.Right },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.Down },
        { (int)NoteCutDirection.None, (int)NoteCutDirection.None }
    };

    public void OnUpdateNoteDirection(InputAction.CallbackContext context)
    {
        if (!context.performed
            || !IsHovering
            || HoveredObject.Dragged
            || (Keyboard.current != null && Keyboard.current.shiftKey.isPressed))
            return;

        var shiftForward = context.GetScrollDirection(Settings.Instance.InvertScrollNoteAngle);
        ScrollUpdateDirection(HoveredObject, shiftForward);
    }

    public void OnUpdateNotePreciseDirection(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var shiftForward = context.GetScrollDirection(Settings.Instance.InvertScrollNoteAngle);
        ScrollPreciseUpdateDirection(HoveredObject, shiftForward);
    }

    public void ScrollUpdateDirection(NoteContainer note, int direction)
    {
        var cutDirection =
            (direction > 0 ? cutDirectionMovedBackward : cutDirectionMovedForward)[note.NoteData.CutDirection];
        NoteCommand.SetCutDirection(
            note.NoteData,
            cutDirection,
            preserveAngleOffset: Settings.Instance.MapVersion >= 3);
    }

    public void ScrollPreciseUpdateDirection(NoteContainer note, int direction)
    {
        // V2 note unsupported. Could implement either ME or NE for V2 note.
        if (Settings.Instance.MapVersion < 3) return;

        var precision = scrollPrecisionController.GetCurrentAngleOffsetPrecision();
        var value = (int)(Mathf.Round((note.NoteData.AngleOffset + (direction * precision)) * 1_000f) / 1_000f);
        var angleOffset = (int)Mathf.Repeat(value, 360);

        NoteCommand.SetAngleOffset(note.NoteData, angleOffset);
    }
}
