using System;

[Serializable]
public sealed class BloomFogParams
{
    public float Offset;
    public float Height;
    public float StartY;
    public float Attenuation;
    public float AutoExposureLimit;
    public bool LegacyAutoExposure;

    [UnityEngine.SerializeField] private bool defaultsCaptured;
    [UnityEngine.SerializeField] private float defaultOffset;
    [UnityEngine.SerializeField] private float defaultHeight;
    [UnityEngine.SerializeField] private float defaultStartY;
    [UnityEngine.SerializeField] private float defaultAttenuation;
    [UnityEngine.SerializeField] private float defaultAutoExposureLimit;
    [UnityEngine.SerializeField] private bool defaultLegacyAutoExposure;

    public void CaptureDefaults()
    {
        defaultOffset = Offset;
        defaultHeight = Height;
        defaultStartY = StartY;
        defaultAttenuation = Attenuation;
        defaultAutoExposureLimit = AutoExposureLimit;
        defaultLegacyAutoExposure = LegacyAutoExposure;
        defaultsCaptured = true;
    }

    public void ResetToDefaults()
    {
        if (!defaultsCaptured)
        {
            CaptureDefaults();
            return;
        }

        Offset = defaultOffset;
        Height = defaultHeight;
        StartY = defaultStartY;
        Attenuation = defaultAttenuation;
        AutoExposureLimit = defaultAutoExposureLimit;
        LegacyAutoExposure = defaultLegacyAutoExposure;
    }
}
