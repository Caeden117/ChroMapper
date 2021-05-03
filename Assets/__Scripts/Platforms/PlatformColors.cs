using System;
using UnityEngine;

[Serializable]
public class PlatformColors
{
    public Color RedColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    public Color BlueColor = BeatSaberSong.DEFAULT_RIGHTCOLOR;
    public Color RedBoostColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    public Color BlueBoostColor = BeatSaberSong.DEFAULT_RIGHTCOLOR;
    public Color RedNoteColor = BeatSaberSong.DEFAULT_LEFTNOTE;
    public Color BlueNoteColor = BeatSaberSong.DEFAULT_RIGHTNOTE;
    public Color ObstacleColor = BeatSaberSong.DEFAULT_LEFTNOTE;

    public PlatformColors Clone()
    {
        return new PlatformColors
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
}
