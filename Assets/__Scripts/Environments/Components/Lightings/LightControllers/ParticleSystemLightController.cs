using UnityEngine;

public class ParticleSystemLightController : LightController
{
    [SerializeField] public ParticleSystem ParticleSystem;
    [SerializeField] public bool SetOnlyOnce;
    [SerializeField] public bool SetColorOnly;
    [SerializeField] public float Intensity = 1f;
    [SerializeField] public float MinAlpha;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.Particle[] particles;

    protected override bool Initialize()
    {
        if (ParticleSystem == null) ParticleSystem = GetComponent<ParticleSystem>();
        if (ParticleSystem == null) return false;

        mainModule = ParticleSystem.main;
        particles = new ParticleSystem.Particle[mainModule.maxParticles];
        return true;
    }

    public override void SetColor(Color color)
    {
        if (!HasInitialized) return;
        Color = color;
        color.a = SetColorOnly ? mainModule.startColor.color.a : Mathf.Max(MinAlpha, color.a * Intensity);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(color);
        ParticleSystem.GetParticles(particles, particles.Length);
        for (var i = 0; i < ParticleSystem.particleCount; i++) particles[i].startColor = color;

        ParticleSystem.SetParticles(particles, ParticleSystem.particleCount);
        if (SetOnlyOnce) enabled = false;
    }
}
