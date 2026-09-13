using System;
using UnityEngine;

public class ColorArrayData : LightController
{
    public event Action<int, Color> OnColorChanged;
    public int Index;

    protected override bool Initialize() => true;

    public override void SetColor(Color color)
    {
        Color = color;
        OnColorChanged?.Invoke(Index, color);
    }
}
