using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemSyncController : SyncController
{
    private ParticleSystem _particleSystem = null!;

    public override void ResetTime() => Sync(0f);

    public override void Sync(float speed)
    {
        if (_particleSystem.isStopped) return;

        // Writing simulationSpeed during playback disables Unity's procedural simulation
        // and replaces its predictive bounds with bounds that can miss shader-driven meshes.
        // Simulate advances the song clock without changing particle properties, then pauses.
        if (speed > 0f)
            _particleSystem.Simulate(speed * Time.deltaTime, false, false, false);
        else if (_particleSystem.isPlaying)
            _particleSystem.Pause(false);
    }

    private void Awake() => _particleSystem = GetComponent<ParticleSystem>();

    private void OnEnable()
    {
        // A child activated by a native Animator starts now, not at its prefab's spawn time.
        // SongSynchronize still supplies the spawn offset for already-active systems.
        if (Atsc != null) SongTime = Atsc.CurrentSeconds;
    }
}
