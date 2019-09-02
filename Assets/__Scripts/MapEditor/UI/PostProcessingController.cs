using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PostProcessingController : MonoBehaviour {

    public PostProcessVolume PostProcess;

    public void TogglePostProcess(bool state)
    {
        PostProcess.enabled = state;
    }

    public void ToggleExtraPostProcess(bool extraPostProcess)
    {
        if (extraPostProcess) PostProcess.profile.GetSetting<Bloom>().intensity.value = 5;
        else PostProcess.profile.GetSetting<Bloom>().intensity.value = 1;
    }

}
