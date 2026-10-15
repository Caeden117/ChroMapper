using System;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    public static readonly Color ColorDefault = new(0.33f, 0.33f, 0.33f, 1f);
    public static readonly Color ColorHighContrast = new(0f, 0f, 0f, 1f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    [SerializeField] public Vector4 ZLineSpacing = new(1f, 1f / 4f, 1f / 8f, 1f / 16f);
    [SerializeField] public Vector4 ZLineThickness = new(0.1f, 0.05f, 0.025f, 0.0125f);

    private static readonly int currentHjdShaderID = Shader.PropertyToID("_CurrentHJD");
    private static readonly int displayHjdLineID = Shader.PropertyToID("_DisplayHJDLine");

    public event Action<Vector4> OnBeatSpacingChanged;
    public event Action<Color> OnXYInterfaceColorChanged;
    public event Action<Color> OnXZInterfaceColorChanged;
    public event Action<float> OnLengthChanged;
    public event Action<Vector4> OnBeatThicknessChanged;

    private void Awake()
    {
        atsc.OnGridMeasureSnappingChanged += HandleGridMeasureSnappingChanged;
        vNjsProvider.OnChanged += UpdateHJDLine;
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.GridTransparency), UpdateInterfaceXZ);
        Settings.NotifyBySettingName(nameof(Settings.InterfaceOpacity), UpdateInterfaceXY);
        Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);
        Settings.NotifyBySettingName(nameof(Settings.OneBeatWidth), UpdateOneBeat);
        Settings.NotifyBySettingName(nameof(Settings.DisplayHJDLine), UpdateDisplayHJDLine);

        UpdateDisplayHJDLine(Settings.Instance.DisplayHJDLine);
        UpdateOneBeat(Settings.Instance.OneBeatWidth);
    }

    private void OnDestroy()
    {
        atsc.OnGridMeasureSnappingChanged -= HandleGridMeasureSnappingChanged;
        vNjsProvider.OnChanged -= UpdateHJDLine;
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
        Settings.ClearSettingNotifications(nameof(Settings.InterfaceOpacity));
        Settings.ClearSettingNotifications(nameof(Settings.TrackLength));
        Settings.ClearSettingNotifications(nameof(Settings.OneBeatWidth));
        Settings.ClearSettingNotifications(nameof(Settings.DisplayHJDLine));
    }

    private void HandleGridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = CMMath.GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        ZLineSpacing[0] = 1f;
        ZLineSpacing[1] = 1f / gridSeparation;

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        ZLineSpacing[2] = useDetailedSegments ? 1f / gridSeparation : 0f;

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= CMMath.GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        ZLineSpacing[3] = usePreciseSegments ? 1f / gridSeparation : 0f;

        OnBeatSpacingChanged?.Invoke(ZLineSpacing);
        UpdateInterfaceXZ();
    }

    private void UpdateInterfaceXY(object _ = null)
    {
        var newColor = Color.white.WithAlpha(Settings.Instance.InterfaceOpacity);
        OnXYInterfaceColorChanged?.Invoke(newColor);
    }

    private void UpdateInterfaceXZ(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? ColorHighContrast : ColorDefault;
        newColor.a = Mathf.Clamp01(1f - gridAlpha);
        OnXZInterfaceColorChanged?.Invoke(newColor);
    }

    private void UpdateTrackLength(object _ = null) =>
        OnLengthChanged?.Invoke(Settings.Instance.TrackLength * EditorScaleController.EditorScale);

    private void UpdateOneBeat(object value)
    {
        ZLineThickness[0] = (float)value;
        OnBeatThicknessChanged?.Invoke(ZLineThickness);
    }

    private void HandleLevelLoaded() => Shader.SetGlobalFloat(currentHjdShaderID, vNjsProvider.BaseHalfJumpDurationInBeats);

    private void UpdateHJDLine() => Shader.SetGlobalFloat(currentHjdShaderID, vNjsProvider.HalfJumpDurationInBeats);
    private void UpdateDisplayHJDLine(object value) => Shader.SetGlobalInt(displayHjdLineID, (bool)value ? 1 : 0);
}
