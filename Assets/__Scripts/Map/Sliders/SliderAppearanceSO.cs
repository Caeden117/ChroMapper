using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SliderAppearanceSO", menuName = "Map/Appearance/Slider Appearance SO")]
public class SliderAppearanceSO : ScriptableObject
{

    public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
    public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

    public void UpdateColor(Color red, Color blue)
    {
        RedColor = red;
        BlueColor = blue;
    }
    
    public void SetSliderAppearance(BeatmapSliderContainer slider)
    {
        switch (slider.SliderData.C)
        {
            case BeatmapNote.NoteTypeA:
                slider.SetColor(RedColor);
                break;
            case BeatmapNote.NoteTypeB:
                slider.SetColor(BlueColor);
                break;
            default:
                break;
        }
    }
}
