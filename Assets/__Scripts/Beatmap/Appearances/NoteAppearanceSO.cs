using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Note Appearance SO", fileName = "NoteAppearanceSO")]
    public class NoteAppearanceSO : ScriptableObject
    {
        [SerializeField] private GameObject notePrefab;

        [Space(10)] [SerializeField] private Sprite unknownSprite;

        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite dotSprite;

        [Space(10)] [SerializeField] private Material unknownNoteMaterial;

        [Space(10)] [SerializeField] private Material blueNoteSharedMaterial;

        [SerializeField] private Material redNoteSharedMaterial;

        [Space(20)] [Header("ChromaToggle Notes")] [SerializeField]
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

        public void SetNoteAppearance(NoteContainer note)
        {
            if (note.NoteData.Type != (int)NoteType.Bomb)
            {
                if (note.NoteData.CutDirection != (int)NoteCutDirection.Any)
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
                switch (note.NoteData.Type)
                {
                    case (int)NoteType.Red:
                        note.SetColor(RedColor);
                        break;
                    case (int)NoteType.Blue:
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

            if (note.NoteData.CustomColor != null)
                note.SetColor(note.NoteData.CustomColor);

            note.Animator.SetData(note.NoteData);
        }
    }
}
