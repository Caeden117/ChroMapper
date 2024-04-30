using UnityEngine;

public class SpectrogramSettingsUpdater : MonoBehaviour
{
    private static readonly int spectrogramBilinearFiltering = Shader.PropertyToID("_Spectrogram_BilinearFiltering");
    private static readonly int spectrogramShift = Shader.PropertyToID("_Spectrogram_Shift");
    
    private void Start()
    {
        Settings.NotifyBySettingName(nameof(Settings.SpectrogramBilinearFiltering), UpdateSpectrogramSettings);
        Settings.NotifyBySettingName(nameof(Settings.SpectrogramShift), UpdateSpectrogramSettings);
        
        UpdateSpectrogramSettings(null);
    }

    private void UpdateSpectrogramSettings(object _)
    {
        Shader.SetGlobalFloat(spectrogramBilinearFiltering, Settings.Instance.SpectrogramBilinearFiltering ? 1 : 0);
        Shader.SetGlobalFloat(spectrogramShift, Settings.Instance.SpectrogramShift);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.SpectrogramBilinearFiltering));
        Settings.ClearSettingNotifications(nameof(Settings.SpectrogramShift));
    }
}
