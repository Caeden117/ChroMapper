using UnityEngine;

[CreateAssetMenu(fileName = "NoteAppearanceSO", menuName = "Map/Appearance/Note Appearance SO")]
public class NoteAppearanceSO : ScriptableObject {
    
    [SerializeField] private GameObject notePrefab;
    [Space(10)]
    [SerializeField] private Sprite unknownSprite;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite dotSprite;
    [Space(10)]
    [SerializeField] private Material unknownNoteMaterial;
    [Space(10)]
    [SerializeField] private Material blueNoteSharedMaterial;
    [SerializeField] private Material redNoteSharedMaterial;
    [Space(20)]
    [Header("ChromaToggle Notes")]
    [SerializeField] private Sprite deflectSprite;
    [Space(10)]
    [SerializeField] private Material greenNoteSharedMaterial;
    [SerializeField] private Material magentaNoteSharedMaterial;
    [Space(10)]
    [SerializeField] private Material monochromeSharedNoteMaterial;
    [SerializeField] private Material duochromeSharedNoteMaterial;
    [Space(10)]
    [SerializeField] private Material superNoteSharedMaterial;

    public Color RedColor { get; private set; } = BeatSaberSong.DEFAULT_LEFTNOTE;
    public Color BlueColor{ get; private set; } = BeatSaberSong.DEFAULT_RIGHTNOTE;

    public void UpdateColor(Color red, Color blue)
    {
        RedColor = red;
        BlueColor = blue;
    }

    public void SetNoteAppearance(BeatmapNoteContainer note) {
        if (note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB)
        {
            if (note.mapNoteData._cutDirection != BeatmapNote.NOTE_CUT_DIRECTION_ANY)
            {
                note.SetArrowVisible(true);
                note.SetDotVisible(false);
            }
            else
            {
                note.SetArrowVisible(false);
                note.SetDotVisible(true);
            }

            //Since sometimes the user can hover over the note grid before all the notes are loading,
            //we create material instances here to prevent NullReferenceExceptions.
            switch (note.mapNoteData._type)
            {
                case BeatmapNote.NOTE_TYPE_A:
                    note.SetColor(RedColor);
                    break;
                case BeatmapNote.NOTE_TYPE_B:
                    note.SetColor(BlueColor);
                    break;
                default:
                    note.SetColor(null);
                    break;
            }
        }
        else
        {
            note.SetArrowVisible(false);
            note.SetDotVisible(false);
            note.SetColor(null);
        }
        if (note.mapNoteData._customData?.HasKey("_color") ?? false)
        {
            note.SetColor(note.mapNoteData._customData["_color"]);
        }
    }
}
