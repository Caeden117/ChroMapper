using System;
using Unity.Collections;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradient : BloomPrePassBackgroundTextureGradient
{
    [SerializeField] public Element[] Elements;

    protected override void UpdatePixels(NativeArray<Color32> pixels, int numberOfPixels)
    {
        for (var i = 0; i < numberOfPixels; i++) pixels[i] = EvaluateColor(i / (float)(numberOfPixels - 1));
    }

    private Color EvaluateColor(float t)
    {
        for (var i = Elements.Length - 2; i >= 0; i--)
        {
            var e1 = Elements[i];
            if (!(t >= e1.StartT)) continue;
            var e2 = Elements[i + 1];
            return Color.LerpUnclamped(
                e1.Color,
                e2.Color,
                Mathf.Pow((t - e1.StartT) / (e2.StartT - e1.StartT), e1.Exp));
        }

        return Elements[^1].Color;
    }

    [Serializable]
    public class Element
    {
        public Color Color;
        public float StartT;
        public float Exp;
    }
}
