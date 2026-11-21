using System;
using Beatmap.Shared;
using UnityEngine;

public class LightColorTween
{
    public float StartTimeAlpha;
    public float StartTimeColor;
    public Color StartColor;
    public float StartAlpha;
    public float StartStrobeFrequency;
    public float StartStrobeBrightness;
    public float StartStrobePulseWidth = 0.5f;
    public Color StartStrobeColor;

    public float EndTimeAlpha;
    public float EndTimeColor;
    public Color EndColor;
    public float EndAlpha;
    public float EndStrobeFrequency;
    public float EndStrobeBrightness;
    public float EndStrobePulseWidth = 0.5f;
    public Color EndStrobeColor;

    public bool StrobeFade;

    public BasicEventColorLerpType ColorLerpType;
    public Func<float, float> Easing = global::Easing.ByName["easeLinear"];
    public Func<float, float> ColorEasing;
    public Func<float, float> StrobeColorEasing;
    public Func<float, float> StrobeEasing;
    // Cache the strobe curve so light updates do not look it up every frame.
    public Vector4 EasingShaderIds;
    public bool ComposeAlphaAtColorEndpoints;

    public Color Color;

    public bool UpdateTime(float time)
    {
        var nTimeAlpha = Mathf.InverseLerp(StartTimeAlpha, EndTimeAlpha, time);
        var nTimeColor = Mathf.InverseLerp(StartTimeColor, EndTimeColor, time);
        var easedColorTime = (ColorEasing ?? Easing)(nTimeColor);
        var color = ComposeAlphaAtColorEndpoints
            ? BasicEventColorLerp.InterpolateWithBrightness(
                StartColor,
                StartAlpha,
                EndColor,
                EndAlpha,
                easedColorTime,
                ColorLerpType)
            : BasicEventColorLerp.Interpolate(
                StartColor,
                EndColor,
                easedColorTime,
                ColorLerpType);
        var alpha = Mathf.LerpUnclamped(StartAlpha, EndAlpha, Easing(nTimeAlpha));

        if (StartStrobeFrequency > 0 || EndStrobeFrequency > 0)
        {
            var strobePulseWidth = Mathf.Lerp(StartStrobePulseWidth, EndStrobePulseWidth, nTimeAlpha);
            var duration = EndTimeAlpha - StartTimeAlpha;
            var elapsed = Mathf.Clamp(time - StartTimeAlpha, 0f, duration);
            var elapsedHalf = elapsed * elapsed / (2f * duration);

            var phase = (((0f - StartStrobeFrequency) * elapsedHalf)
                    + (StartStrobeFrequency * elapsed)
                    + (EndStrobeFrequency * elapsedHalf))
                % 1f;

            // Start a newly enabled strobe at phase zero. At zero frequency its two colors already match, so
            // this offset does not change the transition endpoints.
            if (StrobeFade && StartStrobeFrequency <= 0f && EndStrobeFrequency > 0f)
            {
                phase = Mathf.Repeat(phase - ((StartStrobeFrequency + EndStrobeFrequency) * duration * 0.5f), 1f);
            }

            // Interpolate strobe color between start and end
            var startStrobeColor = StartStrobeColor;
            var endStrobeColor = EndStrobeColor;
            // If no explicit strobe color, fall back to normal color
            if (startStrobeColor == Color.clear && endStrobeColor == Color.clear)
            {
                startStrobeColor = StartColor;
                endStrobeColor = EndColor;
            }

            var strobeColor = BasicEventColorLerp.Interpolate(
                startStrobeColor,
                endStrobeColor,
                (StrobeColorEasing ?? Easing)(nTimeColor),
                ColorLerpType);
            strobeColor.a = Mathf.LerpUnclamped(
                startStrobeColor.a * StartStrobeBrightness,
                endStrobeColor.a * EndStrobeBrightness,
                Easing(nTimeAlpha));

            if (!ComposeAlphaAtColorEndpoints)
                color.a *= alpha;

            if (StrobeFade)
            {
                var fade = global::Easing.StrobeFade(phase, strobePulseWidth, StrobeEasing ?? global::Easing.Cubic.InOut);
                color = BasicEventColorLerp.InterpolateStrobeFade(color, strobeColor, fade, ColorLerpType);
            }
            else if (phase >= strobePulseWidth)
            {
                color = strobeColor;
            }
            // else // off phase: base color already scaled by brightness, nothing to do here.
        }
        else if (!ComposeAlphaAtColorEndpoints)
            color.a *= alpha;

        if (Color == color) return false;
        Color = color;
        return true;
    }

}
