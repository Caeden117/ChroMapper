using System;
using TMPro;

[Obsolete("Undo/Redo is disabled when node editor is open anyway")]
public class NodeEditorTextChangedAction : BeatmapAction
{
    private TMP_InputField inputField;
    public string CurrentText { get; private set; }
    public string OldText { get; private set; }

    private int currentCaret;
    private int oldCaret;

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
