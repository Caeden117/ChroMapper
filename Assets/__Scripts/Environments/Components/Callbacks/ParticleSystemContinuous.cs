using UnityEngine;

public class ParticleSystemContinuous : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect Effect;
    [SerializeField] public ParticleSystem[] ParticleSystems;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        var p = Effect.GetCurrentState();
        if (p.index != -1) HandleStateChanged(p);
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged((int index, BasicEventStateData state) data) =>
        ToggleEmitting(data.state.Base.Value == 1);

    private void ToggleEmitting(bool shouldPlay)
    {
        if (shouldPlay)
        {
            for (var i = 0; i < ParticleSystems.Length; i++) ParticleSystems[i].Play(false);
        }
        else
        {
            for (var i = 0; i < ParticleSystems.Length; i++)
                ParticleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
