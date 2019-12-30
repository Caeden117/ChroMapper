using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

public class PostProcessingController : MonoBehaviour {

    public PostProcessVolume PostProcess;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;
    [SerializeField] private Toggle chromaticAberration;

    private void Start()
    {
        float v = Settings.Instance.PostProcessingIntensity;
        intensitySlider.value = v * 2;
        intensityLabel.text = v.ToString();
        PostProcess.profile.GetSetting<Bloom>().intensity.value = v;

        chromaticAberration.isOn = Settings.Instance.ChromaticAberration;
    }

    public void UpdatePostProcessIntensity(float v)
    {
        PostProcess.profile.GetSetting<Bloom>().intensity.value = v / 2;
        intensityLabel.text = (v / 2).ToString();
        Settings.Instance.PostProcessingIntensity = v / 2;
    }

    public void UpdateChromaticAberration(bool enabled)
    {
        PostProcess.profile.GetSetting<ChromaticAberration>().active = enabled;
        Settings.Instance.ChromaticAberration = enabled;
    }
}
