using System;
using TMPro;

[Obsolete("Undo/Redo is disabled when node editor is open anyway")]
public class NodeEditorTextChangedAction : BeatmapAction
{
    private readonly int currentCaret;
    private readonly TMP_InputField inputField;
    private readonly int oldCaret;

    public NodeEditorTextChangedAction(string currentText, int currentPosition,
        string oldText, int oldPosition,
        TMP_InputField inputField) : base(null)
    {
        currentCaret = currentPosition;
        oldCaret = oldPosition;
        CurrentText = currentText;
        OldText = oldText;
        this.inputField = inputField;
    }

    public string CurrentText { get; }
    public string OldText { get; }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        inputField.text = CurrentText;
        inputField.caretPosition = currentCaret;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        inputField.text = OldText;
        inputField.caretPosition = oldCaret;
    }
}
