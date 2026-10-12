using UnityEngine;

// Environment layers are shared with unrelated objects, so smoke and particle visibility is controlled per renderer.
// Each particle system uses forceRenderingOff so setting changes also apply to cloned and pooled effects without re-enabling renderers disabled by the environment or other controllers.
[RequireComponent(typeof(ParticleSystemRenderer))]
[DisallowMultipleComponent]
public class WorldParticleVisibility : MonoBehaviour
{
    private static readonly Shader particlesShader = Shader.Find("ChroMapper/Particles");
    private static readonly int fogTypeId = Shader.PropertyToID("_FogType");

    private ParticleSystemRenderer particleRenderer;
    private string settingName;

    public static void Initialize(Transform root)
    {
        foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (!renderer.TryGetComponent<WorldParticleVisibility>(out _))
                renderer.gameObject.AddComponent<WorldParticleVisibility>();
    }

    private void Awake()
    {
        particleRenderer = GetComponent<ParticleSystemRenderer>();
        settingName = nameof(Settings.EditModeWorldParticles);
        foreach (var material in particleRenderer.sharedMaterials)
        {
            if (material == null || material.shader != particlesShader || material.GetFloat(fogTypeId) != 2f)
                continue;

            settingName = nameof(Settings.EditModeSmoke);
            break;
        }
    }

    private void OnEnable()
    {
        UIMode.OnUIModeSwitched += UpdateVisibility;
        Settings.NotifyBySettingName(settingName, UpdateSetting);
        UpdateVisibility(UIMode.SelectedMode);
    }

    private void OnDisable()
    {
        UIMode.OnUIModeSwitched -= UpdateVisibility;
        Settings.StopNotifyingBySettingName(settingName, UpdateSetting);
        particleRenderer.forceRenderingOff = false;
    }

    private void UpdateSetting(object value) => UpdateVisibility(UIMode.SelectedMode);

    private void UpdateVisibility(UIModeType mode)
    {
        var showInEditMode = settingName == nameof(Settings.EditModeSmoke)
            ? Settings.Instance.EditModeSmoke
            : Settings.Instance.EditModeWorldParticles;
        particleRenderer.forceRenderingOff = !showInEditMode && mode is not (UIModeType.Playing or UIModeType.Preview);
    }
}
