using UnityEngine;

[CreateAssetMenu(fileName = "NoteAppearanceSO", menuName = "Map/Appearance/Note Appearance SO")]
public class NoteAppearanceSO : ScriptableObject
{
    [SerializeField] private GameObject notePrefab;

    [Space(10)] [SerializeField] private Sprite unknownSprite;

    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Sprite dotSprite;

    [Space(10)] [SerializeField] private Material unknownNoteMaterial;

    [Space(10)] [SerializeField] private Material blueNoteSharedMaterial;

    [SerializeField] private Material redNoteSharedMaterial;

    [Space(20)]
    [Header("ChromaToggle Notes")]
    [SerializeField]
    private Sprite deflectSprite;

    [Space(10)] [SerializeField] private Material greenNoteSharedMaterial;

    [SerializeField] private Material magentaNoteSharedMaterial;

    [Space(10)] [SerializeField] private Material monochromeSharedNoteMaterial;

    [SerializeField] private Material duochromeSharedNoteMaterial;

    [Space(10)] [SerializeField] private Material superNoteSharedMaterial;

    public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
    public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

    public void UpdateColor(Color red, Color blue)
    {
        RedColor = red;
        BlueColor = blue;
    }

    public void SetNoteAppearance(BeatmapNoteContainer note)
    {
        if (note.MapNoteData.Type != BeatmapNote.NoteTypeBomb)
        {
            if (note.MapNoteData.CutDirection != BeatmapNote.NoteCutDirectionAny)
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
            switch (note.MapNoteData.Type)
            {
                case BeatmapNote.NoteTypeA:
                    note.SetColor(RedColor);
                    break;
                case BeatmapNote.NoteTypeB:
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

        if (note.MapNoteData.CustomData?.HasKey("_color") ?? false)
            note.SetColor(note.MapNoteData.CustomData["_color"]);
    }
}
