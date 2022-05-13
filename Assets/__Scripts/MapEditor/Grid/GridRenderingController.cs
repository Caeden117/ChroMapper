using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    private static readonly int offset = Shader.PropertyToID("_Offset");
    private static readonly int gridSpacing = Shader.PropertyToID("_GridSpacing");
    private static readonly int mainColor = Shader.PropertyToID("_Color");
    private static readonly Color mainColorDefault = new Color(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color mainColorHighContrast = new Color(0f, 0f, 0f, 1f);

    private static MaterialPropertyBlock oneBeatPropertyBlock;
    private static MaterialPropertyBlock smallBeatPropertyBlock;
    private static MaterialPropertyBlock detailedBeatPropertyBlock;
    private static MaterialPropertyBlock preciseBeatPropertyBlock;
    private static MaterialPropertyBlock beatColorPropertyBlock;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Renderer[] oneBeat;
    [SerializeField] private Renderer[] smallBeatSegment;
    [SerializeField] private Renderer[] detailedBeatSegment;
    [SerializeField] private Renderer[] preciseBeatSegment;
    [SerializeField] private Renderer[] opaqueGrids;
    [SerializeField] private Renderer[] transparentGrids;
    [SerializeField] private Transform[] gridFrontTransforms;

    private readonly List<Renderer> allRenderers = new List<Renderer>();

    private void Awake()
    {
        oneBeatPropertyBlock = new MaterialPropertyBlock();
        smallBeatPropertyBlock = new MaterialPropertyBlock();
        detailedBeatPropertyBlock = new MaterialPropertyBlock();
        preciseBeatPropertyBlock = new MaterialPropertyBlock();
        beatColorPropertyBlock = new MaterialPropertyBlock();

        atsc.GridMeasureSnappingChanged += GridMeasureSnappingChanged;
        allRenderers.AddRange(oneBeat);
        allRenderers.AddRange(smallBeatSegment);
        allRenderers.AddRange(detailedBeatSegment);
        allRenderers.AddRange(preciseBeatSegment);
        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateGridColors);
        Settings.NotifyBySettingName(nameof(Settings.GridTransparency), UpdateGridColors);
        Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);
    }

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= GridMeasureSnappingChanged;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
        Settings.ClearSettingNotifications(nameof(Settings.TrackLength));
    }

    public void UpdateOffset(float offset)
    {
        Shader.SetGlobalFloat(GridRenderingController.offset, offset);
        if (!atsc.IsPlaying) GridMeasureSnappingChanged(atsc.GridMeasureSnapping);
    }

    private void GridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        oneBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f);
        foreach (var g in oneBeat) g.SetPropertyBlock(oneBeatPropertyBlock);

        smallBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in smallBeatSegment) g.SetPropertyBlock(smallBeatPropertyBlock);

        var useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        detailedBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in detailedBeatSegment)
        {
            g.enabled = useDetailedSegments;
            g.SetPropertyBlock(detailedBeatPropertyBlock);
        }

        var usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        preciseBeatPropertyBlock.SetFloat(gridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (var g in preciseBeatSegment)
        {
            g.enabled = usePreciseSegments;
            g.SetPropertyBlock(preciseBeatPropertyBlock);
        }

        UpdateGridColors();
    }

    private void UpdateGridColors(object _ = null)
    {
        var gridAlpha = Settings.Instance.GridTransparency;
        var newColor = Settings.Instance.HighContrastGrids ? mainColorHighContrast : mainColorDefault;
        newColor.a = 1f - gridAlpha;
        beatColorPropertyBlock.SetColor(mainColor, newColor);
        foreach (var g in transparentGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = !(newColor.a == 1f);
        }

        foreach (var g in opaqueGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = newColor.a == 1f;
        }
    }

    private void UpdateTrackLength(object _) {
        foreach (var trans in gridFrontTransforms)
        {
            var scale = trans.localScale;
            trans.localScale = new Vector3(scale.x, scale.y, Settings.Instance.TrackLength * 4);
        }
    }

    private int GetLowestDenominator(int a)
    {
        if (a <= 1) return 2;

        IEnumerable<int> factors = PrimeFactors(a);

        if (factors.Any()) return factors.Max();
        return a;
    }

    public static List<int> PrimeFactors(int a)
    {
        var retval = new List<int>();
        for (var b = 2; a > 1; b++)
        {
            while (a % b == 0)
            {
                a /= b;
                retval.Add(b);
            }
        }

        return retval;
    }
}
