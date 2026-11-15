using UnityEngine;

public class BurstFireEffect : FireEffect
{
    public float FadeOutDuration = 1f;
    public AnimationCurve FlipbookFadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public AnimationCurve BloomFadeOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

    internal override (float flipbookAlpha, float bloomAlpha) InitialAlphas =>
        (FlipbookFadeOutCurve.Evaluate(1f), BloomFadeOutCurve.Evaluate(1f));
}
