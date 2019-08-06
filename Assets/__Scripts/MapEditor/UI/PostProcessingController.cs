using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class PostProcessingController : MonoBehaviour {

    public PostProcessVolume PostProcess;

    public Toggle PostProcessToggle;
    public Toggle ExtraPostProcessToggle;

    private void Start()
    {
        PostProcessToggle.onValueChanged.AddListener((x) => TogglePostProcess(x));
        ExtraPostProcessToggle.onValueChanged.AddListener((x) => ToggleExtraPostProcess(x));
    }

    private void OnDestroy()
    {
        PostProcessToggle.onValueChanged.RemoveAllListeners();
        ExtraPostProcessToggle.onValueChanged.RemoveAllListeners();
    }

    public void TogglePostProcess(bool state)
    {
        PostProcess.enabled = state;
    }

    public void ToggleExtraPostProcess(bool extraPostProcess)
    {
        switch (extraPostProcess)
        {
            case true:
                PostProcess.profile.GetSetting<ChromaticAberration>().intensity.value = 0.5f;
                PostProcess.profile.GetSetting<Bloom>().softKnee.value = 0.5f;
                PostProcess.profile.GetSetting<Bloom>().dirtIntensity.value = 15;
                break;
            case false:
                PostProcess.profile.GetSetting<ChromaticAberration>().intensity.value = 0.1f;
                PostProcess.profile.GetSetting<Bloom>().softKnee.value = 0.25f;
                PostProcess.profile.GetSetting<Bloom>().dirtIntensity.value = 0;
                break;
        }
    }

}
