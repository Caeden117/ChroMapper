using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PostProcessingController : MonoBehaviour
{
    public Volume PostProcess;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;
    [SerializeField] private Toggle chromaticAberration;

    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.PostProcessingIntensity), UpdatePostProcessIntensity);
        Settings.NotifyBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);

        UpdatePostProcessIntensity(Settings.Instance.PostProcessingIntensity);
        UpdateChromaticAberration(Settings.Instance.ChromaticAberration);
    }

    public void UpdatePostProcessIntensity(object o)
    {
        var v = Convert.ToSingle(o);
        PostProcess.profile.TryGet(out Bloom bloom);
        bloom.intensity.value = v;
    }

    public void UpdateChromaticAberration(object o)
    {
        var enabled = Convert.ToBoolean(o);
        PostProcess.profile.TryGet(out ChromaticAberration ca);
        ca.active = enabled;
    }
}
