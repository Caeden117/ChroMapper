using UnityEngine;

/// <summary>Subdivision stepping for Tempo Mapper; the 3D timeline controller remains independent.</summary>
public static class TempoCursorPrecision
{
    public static int Step(int current, float scroll, bool precise)
    {
        if (scroll == 0) return current;
        var increase = (scroll > 0) != Settings.Instance.InvertPrecisionScroll;
        return Mathf.Clamp(precise ? current + (increase ? 1 : -1)
            : Mathf.RoundToInt(current * (increase ? 2f : 0.5f)), 1, 64);
    }
}
