using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

public class PostProcessingController : MonoBehaviour {

    public PostProcessVolume PostProcess;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityLabel;

    private void Start()
    {
        float v = Settings.Instance.PostProcessingIntensity;
        intensitySlider.value = v * 2;
        intensityLabel.text = v.ToString();
        PostProcess.profile.GetSetting<Bloom>().intensity.value = v;
    }

    public void UpdatePostProcessIntensity(float v)
    {
        PostProcess.profile.GetSetting<Bloom>().intensity.value = v / 2;
        intensityLabel.text = (v / 2).ToString();
        Settings.Instance.PostProcessingIntensity = v / 2;
    }
}
