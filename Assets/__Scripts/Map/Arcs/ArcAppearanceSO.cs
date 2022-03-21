using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArcAppearanceSO", menuName = "Map/Appearance/Arc Appearance SO")]
public class ArcAppearanceSO : ScriptableObject
{

    public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
    public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

    public void UpdateColor(Color red, Color blue)
    {
        RedColor = red;
        BlueColor = blue;
    }
    
    public void SetArcAppearance(BeatmapArcContainer arc)
    {
        switch (arc.ArcData.Color)
        {
            case BeatmapNote.NoteTypeA:
                arc.SetColor(RedColor);
                break;
            case BeatmapNote.NoteTypeB:
                arc.SetColor(BlueColor);
                break;
            default:
                break;
        }
    }
}
