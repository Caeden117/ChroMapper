using System;
using UnityEngine;

[Serializable]
public class PlatformColors
{
    public Color RedColor = BeatSaberSong.DefaultLeftColor;
    public Color BlueColor = BeatSaberSong.DefaultRightColor;
    public Color RedBoostColor = BeatSaberSong.DefaultLeftColor;
    public Color BlueBoostColor = BeatSaberSong.DefaultRightColor;
    public Color RedNoteColor = BeatSaberSong.DefaultLeftNote;
    public Color BlueNoteColor = BeatSaberSong.DefaultRightNote;
    public Color ObstacleColor = BeatSaberSong.DefaultLeftNote;

    public PlatformColors Clone() =>
        new PlatformColors
        {
            RedColor = RedColor,
            BlueColor = BlueColor,
            RedBoostColor = RedBoostColor,
            BlueBoostColor = BlueBoostColor,
            RedNoteColor = RedNoteColor,
            BlueNoteColor = BlueNoteColor,
            ObstacleColor = ObstacleColor
        };
}
