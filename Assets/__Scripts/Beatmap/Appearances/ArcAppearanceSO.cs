using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Arc Appearance SO", fileName = "ArcAppearanceSO")]
    public class ArcAppearanceSO : ScriptableObject
    {
        public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
        public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

        public void UpdateColor(Color red, Color blue)
        {
            RedColor = red;
            BlueColor = blue;
        }

        public void SetArcAppearance(ArcContainer arc)
        {
            switch (arc.ArcData.Color)
            {
                case (int)NoteColor.Red:
                    arc.SetColor(RedColor);
                    break;
                case (int)NoteColor.Blue:
                    arc.SetColor(BlueColor);
                    break;
            }

            if (arc.ArcData.CustomColor != null)
                arc.SetColor((Color)arc.ArcData.CustomColor);
        }
    }
}
