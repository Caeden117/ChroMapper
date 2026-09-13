using UnityEngine;

public class ParticleSystemLightsController : CombinedLightsController
{
    [SerializeField] public ParticleSystem ParticleSystem;
    [SerializeField] public bool SetOnlyOnce;
    [SerializeField] public bool SetColorOnly;
    [SerializeField] public float MinAlpha;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.Particle[] particles;

    protected void Awake()
    {
        mainModule = ParticleSystem.main;
        particles = new ParticleSystem.Particle[mainModule.maxParticles];
    }

    protected override bool Initialize() => ParticleSystem != null;

    public override void SetColor(Color color)
    {
        color.a = SetColorOnly ? mainModule.startColor.color.a : Mathf.Max(MinAlpha, color.a);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(color);
        ParticleSystem.GetParticles(particles, particles.Length);
        for (var i = 0; i < ParticleSystem.particleCount; i++) particles[i].startColor = color;

        ParticleSystem.SetParticles(particles, ParticleSystem.particleCount);
        if (SetOnlyOnce) enabled = false;
    }
}
