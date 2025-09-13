using System;
using UnityEngine;

public struct LightingData
{
    public float StartTime;
    public float StartTimeLocal; // local is used to interpolate flash/fade/chroma gradient
    public int StartValue;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTime;
    public float EndTimeLocal;
    public int EndValue;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing;
    public bool UseHSV;

    // this is far cheaper to do, normally there is no math involved and is always unique
    // but this might backfire if some float weirdness involved especially with how ATSC work
    public static bool operator ==(LightingData m1, LightingData m2) => m1.StartTime == m2.StartTime;
    public static bool operator !=(LightingData m1, LightingData m2) => m1.StartTime != m2.StartTime;
}
