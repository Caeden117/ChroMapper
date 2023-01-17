using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ChainAppearanceSO", menuName = "Map/Appearance/Chain Appearance SO")]
public class ChainAppearanceSO : ScriptableObject
{
    public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
    public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

    public void UpdateColor(Color red, Color blue)
    {
        RedColor = red;
        BlueColor = blue;
    }

    public void SetChainAppearance(BeatmapChainContainer chain)
    {
        switch (chain.ChainData.Color)
        {
            case BeatmapNote.NoteTypeA:
                chain.SetColor(RedColor);
                break;
            case BeatmapNote.NoteTypeB:
                chain.SetColor(BlueColor);
                break;
            default:
                break;
        }
    }
}
