using UnityEngine;

public class ParticleSystemEventController : MonoBehaviour
{
    [SerializeField] public ParticleSystem ParticleSystem;
    [SerializeField] public float FullDuration;

    public float StartTime { get; private set; }
    public float EndTime => StartTime + FullDuration;

    public void Initialize(float startTime)
    {
        StartTime = startTime;
        ParticleSystem.Simulate(0f, true, true);
    }

    public void ManualUpdate(float time, float deltaTime)
    {
        if (deltaTime == 0f) return;
        if (deltaTime > 0f && deltaTime < 1f / 30f)
            ParticleSystem.Simulate(deltaTime, true, false);
        else
            ParticleSystem.Simulate(time - StartTime, true, true);
    }

    public void Stop() => ParticleSystem.Stop();
}
