using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NotePreviewController : MonoBehaviour {

    [SerializeField] private Button[] UIButtons;
    public bool PlacingWall = false;
    internal static NotePreviewController Instance;

    void Start()
    {
        Instance = this;
        if (UIButtons.Length == 0) return;
        foreach(Button button in UIButtons) button.onClick.AddListener(() => UIButtonPress(button.name));
    }

    void UIButtonPress(string name)
    {
        if (!UIButtons[0].interactable) UIButtons[0].interactable = true;
        List<string> splitName = name.Split(' ').ToList();
        splitName.Remove(splitName.Last());
        if (!PlacingWall)
        {
            switch (string.Join(" ", splitName.ToArray()))
            {
                case "Red Note":
                    NotePreview.UpdateChromaNote(false);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_A);
                    break;
                case "Blue Note":
                    NotePreview.UpdateChromaNote(false);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_B);
                    break;
                case "Blue Alt":
                    NotePreview.UpdateChromaNote(true);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_B);
                    NotePreview.UpdateChromaType(BeatmapChromaNote.ALTERNATE);
                    break;
                case "Red Alt":
                    NotePreview.UpdateChromaNote(true);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_A);
                    NotePreview.UpdateChromaType(BeatmapChromaNote.ALTERNATE);
                    break;
                case "Monochrome":
                    NotePreview.UpdateChromaNote(true);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_A);
                    NotePreview.UpdateChromaType(BeatmapChromaNote.MONOCHROME);
                    break;
                case "Duochrome":
                    NotePreview.UpdateChromaNote(true);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_A);
                    NotePreview.UpdateChromaType(BeatmapChromaNote.DUOCHROME);
                    break;
                case "Bomb":
                    NotePreview.UpdateChromaNote(false);
                    NotePreview.UpdateHoverNoteType(BeatmapNote.NOTE_TYPE_BOMB);
                    break;
                case "Wall":
                    PlacingWall = true;
                    Debug.Log("Wall placement enabled.");
                    break;
            }
        }
        else
        {
            if (string.Join(" ", splitName.ToArray()) == "Wall") return;
            PlacingWall = false;
            Debug.Log("Wall placement disabled.");
        }
    }
}
