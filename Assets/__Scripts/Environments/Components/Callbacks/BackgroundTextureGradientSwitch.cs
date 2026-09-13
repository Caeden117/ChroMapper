using UnityEngine;

public class BackgroundTextureGradientSwitch : MonoBehaviour
{
    [SerializeField] public ColorBoostEffect Effect;

    [SerializeField] public BloomPrePassBackgroundTextureGradient DefaultTextureGradient;
    [SerializeField] public BloomPrePassBackgroundTextureGradient BoostTextureGradient;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        HandleStateChanged(Effect.GetCurrentState());
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged(bool boost)
    {
        DefaultTextureGradient.enabled = !boost;
        BoostTextureGradient.enabled = boost;
    }
}
