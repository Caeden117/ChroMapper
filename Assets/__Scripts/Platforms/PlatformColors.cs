using System;
using UnityEngine;

[Serializable]
public class PlatformColors
{
    public Color RedColor = BeatSaberSong.DefaultLeftColor;
    public Color BlueColor = BeatSaberSong.DefaultRightColor;
    public Color WhiteColor = BeatSaberSong.DefaultWhiteColor;
    public Color RedBoostColor = BeatSaberSong.DefaultLeftColor;
    public Color BlueBoostColor = BeatSaberSong.DefaultRightColor;
    public Color WhiteBoostColor = BeatSaberSong.DefaultWhiteColor;
    public Color RedNoteColor = BeatSaberSong.DefaultLeftNote;
    public Color BlueNoteColor = BeatSaberSong.DefaultRightNote;
    public Color ObstacleColor = BeatSaberSong.DefaultLeftNote;

    public PlatformColors Clone() =>
        new PlatformColors
        {
            RedColor = RedColor,
            BlueColor = BlueColor,
            WhiteColor = WhiteColor,
            RedBoostColor = RedBoostColor,
            BlueBoostColor = BlueBoostColor,
            WhiteBoostColor = WhiteBoostColor,
            RedNoteColor = RedNoteColor,
            BlueNoteColor = BlueNoteColor,
            ObstacleColor = ObstacleColor
        };
}
