using System;
using UnityEngine;

[Serializable]
public class PlatformColorScheme
{
    public Color RedColor = DefaultColors.Left;
    public Color BlueColor = DefaultColors.Right;
    public Color WhiteColor = DefaultColors.White;
    public Color RedBoostColor = DefaultColors.Left;
    public Color BlueBoostColor = DefaultColors.Right;
    public Color WhiteBoostColor = DefaultColors.White;
    public Color RedNoteColor = DefaultColors.LeftNote;
    public Color BlueNoteColor = DefaultColors.RightNote;
    public Color ObstacleColor = DefaultColors.LeftNote;

    public PlatformColorScheme Clone() =>
        new PlatformColorScheme
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
