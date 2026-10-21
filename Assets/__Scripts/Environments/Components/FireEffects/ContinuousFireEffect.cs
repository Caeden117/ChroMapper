using UnityEngine;

public class ContinuousFireEffect : FireEffect
{
    public float FadeInDuration = 1f;
    public float FadeOutDuration = 1f;
    public float SustainDuration = 1f;
    public AnimationCurve FlipbookSustainCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    public AnimationCurve BloomSustainCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    internal (float flipbookAlpha, float bloomAlpha) EvaluateEffect(
        float songTime, float startTime, float endTime)
    {
        var fadeInEnd = Mathf.Min(startTime + FadeInDuration, endTime);
        var fadeOutStart = Mathf.Max(endTime - FadeOutDuration, startTime);
        var fadeIn = Mathf.InverseLerp(startTime, fadeInEnd,
            Mathf.Clamp(songTime, startTime, fadeInEnd));
        var fadeOut = 1f - Mathf.InverseLerp(fadeOutStart, endTime,
            Mathf.Clamp(songTime, fadeOutStart, endTime));
        var envelope = Easing.Quadratic.Out(fadeIn) * Easing.Quadratic.InOut(fadeOut);
        var progress = (songTime - startTime) / SustainDuration;
        return (
            envelope * FlipbookSustainCurve.Evaluate(progress),
            envelope * BloomSustainCurve.Evaluate(progress));
    }
}
