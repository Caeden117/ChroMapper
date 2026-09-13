using UnityEngine;

public class ParticleSystemEffect : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect Effect;
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;
    [SerializeField] public bool LightOnStart;

    [SerializeField] public ParticleSystem ParticleSystem;

    private bool lightIsOn;
    private Color offColor = new(0f, 0f, 0f, 0f);
    private float highlightValue;
    private Color afterHighlightColor;
    private Color highlightColor;
    private const float fadeSpeed = 2f;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.Particle[] particles;
    private Color particleColor;

    private void Start()
    {
        Effect.OnStateChanged += HandleStateChanged;
        mainModule = ParticleSystem.main;
        particles = new ParticleSystem.Particle[mainModule.maxParticles];
        lightIsOn = LightOnStart;
        offColor = ColorSchemeProvider.ColorScheme.EnvironmentLeftColor.WithAlpha(0f);
        particleColor = lightIsOn ? ColorSchemeProvider.ColorScheme.EnvironmentLeftColor : offColor;
        RefreshParticles();
        var p = Effect.GetCurrentState();
        if (p.index != -1) HandleStateChanged(p);
        enabled = false;
    }

    private void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    protected void Update()
    {
        if (!lightIsOn && highlightValue == 0f) return;
        particleColor = Color.Lerp(afterHighlightColor, highlightColor, highlightValue);
        highlightValue = Mathf.Lerp(highlightValue, 0f, Time.deltaTime * fadeSpeed);
        if (highlightValue < 0.0001f)
        {
            highlightValue = 0f;
            particleColor = afterHighlightColor;
            enabled = false;
        }

        RefreshParticles();
    }

    private void HandleStateChanged((int index, BasicEventStateData state) data)
    {
        var value = data.state.Base.Value;
        switch (value)
        {
            case 0:
                lightIsOn = false;
                highlightValue = 0f;
                enabled = false;
                particleColor = offColor;
                RefreshParticles();
                break;
            case 1 or 5:
                lightIsOn = true;
                highlightValue = 0f;
                enabled = false;
                offColor =
                    (particleColor = value == 1
                        ? ColorSchemeProvider.ColorScheme.EnvironmentLeftColor
                        : ColorSchemeProvider.ColorScheme.EnvironmentRightColor)
                    .WithAlpha(0f);
                RefreshParticles();
                break;
            case 2 or 6:
                lightIsOn = true;
                highlightValue = 1f;
                enabled = true;
                highlightColor =
                    value == 2
                        ? ColorSchemeProvider.ColorScheme.EnvironmentLeftBoostColor
                        : ColorSchemeProvider.ColorScheme.EnvironmentRightBoostColor;
                offColor = highlightColor.WithAlpha(0f);
                particleColor = highlightColor;
                afterHighlightColor = value == 2
                    ? ColorSchemeProvider.ColorScheme.EnvironmentLeftColor
                    : ColorSchemeProvider.ColorScheme.EnvironmentRightColor;
                break;
            case 3 or 7 or -1:
                lightIsOn = true;
                highlightValue = 1f;
                enabled = true;
                highlightColor =
                    value == 3
                        ? ColorSchemeProvider.ColorScheme.EnvironmentLeftBoostColor
                        : ColorSchemeProvider.ColorScheme.EnvironmentRightBoostColor;
                offColor = highlightColor.WithAlpha(0f);
                particleColor = highlightColor;
                afterHighlightColor = offColor;
                break;
        }
    }

    private void RefreshParticles()
    {
        mainModule.startColor = particleColor;
        ParticleSystem.GetParticles(particles, particles.Length);
        for (var i = 0; i < ParticleSystem.particleCount; i++) particles[i].startColor = particleColor;

        ParticleSystem.SetParticles(particles, ParticleSystem.particleCount);
    }
}
