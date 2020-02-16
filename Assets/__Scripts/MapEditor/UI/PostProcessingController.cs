using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class PostProcessingController : MonoBehaviour {

    public Volume PostProcess;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;
    [SerializeField] private Toggle chromaticAberration;

    private void Start()
    {
        float v = Settings.Instance.PostProcessingIntensity;
        intensitySlider.value = v * 10;
        intensityLabel.text = v.ToString();
        PostProcess.profile.TryGet(out Bloom bloom);
        bloom.intensity.value = v;

        chromaticAberration.isOn = Settings.Instance.ChromaticAberration;
    }

    public void UpdatePostProcessIntensity(float v)
    {
        PostProcess.profile.TryGet(out Bloom bloom);
        bloom.intensity.value = v / 10;
        intensityLabel.text = (v / 10).ToString();
        Settings.Instance.PostProcessingIntensity = v / 10;
    }

    public void UpdateChromaticAberration(bool enabled)
    {
        PostProcess.profile.TryGet(out ChromaticAberration ca);
        ca.active = enabled;
        Settings.Instance.ChromaticAberration = enabled;
    }
}
