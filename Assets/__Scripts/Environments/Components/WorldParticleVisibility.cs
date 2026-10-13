using System.Linq;
using UnityEngine;

// Environment generation attaches one per particle renderer because environment layers are shared with unrelated objects.
// forceRenderingOff applies the edit-mode restriction without overwriting renderer.enabled state owned by map content or another controller.
[RequireComponent(typeof(ParticleSystemRenderer))]
[DisallowMultipleComponent]
public class WorldParticleVisibility : MonoBehaviour
{
    private const string particlesShaderName = "ChroMapper/Particles";
    private static readonly int fogTypeId = Shader.PropertyToID("_FogType");

    [SerializeField] public ParticleSystemRenderer Renderer;
    [SerializeField] public bool IsSmoke;

    public static void Initialize(Transform root)
    {
        foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (!renderer.TryGetComponent<WorldParticleVisibility>(out _))
            {
                renderer.gameObject.AddComponent<WorldParticleVisibility>();
            }
        }
    }

    internal static bool IsSmokeMaterial(ParticleSystemRenderer renderer) =>
        renderer.sharedMaterials.Any(material => material != null
            && material.shader != null
            && material.shader.name == particlesShaderName
            && material.GetFloat(fogTypeId) == 2f);

    private string SettingName => IsSmoke
        ? nameof(Settings.EditModeSmoke)
        : nameof(Settings.EditModeWorldParticles);

    private void Awake()
    {
        if (Renderer != null)
        {
            return;
        }

        // Spawned particle prefabs are not built from environment component data, so classify them when they first appear.
        Renderer = GetComponent<ParticleSystemRenderer>();
        IsSmoke = IsSmokeMaterial(Renderer);
    }

    private void OnEnable()
    {
        UIMode.OnUIModeSwitched += UpdateVisibility;
        Settings.NotifyBySettingName(SettingName, UpdateSetting);
        UpdateVisibility(UIMode.SelectedMode);
    }

    private void OnDisable()
    {
        UIMode.OnUIModeSwitched -= UpdateVisibility;
        Settings.StopNotifyingBySettingName(SettingName, UpdateSetting);
        Renderer.forceRenderingOff = false;
    }

    private void UpdateSetting(object value) => UpdateVisibility(UIMode.SelectedMode);

    private void UpdateVisibility(UIModeType mode)
    {
        var showInEditMode = IsSmoke
            ? Settings.Instance.EditModeSmoke
            : Settings.Instance.EditModeWorldParticles;
        Renderer.forceRenderingOff = !showInEditMode && mode is not (UIModeType.Playing or UIModeType.Preview);
    }
}
