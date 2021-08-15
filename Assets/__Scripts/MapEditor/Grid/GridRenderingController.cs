using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridRenderingController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Renderer[] oneBeat;
    [SerializeField] private Renderer[] smallBeatSegment;
    [SerializeField] private Renderer[] detailedBeatSegment;
    [SerializeField] private Renderer[] preciseBeatSegment;
    [SerializeField] private Renderer[] opaqueGrids;
    [SerializeField] private Renderer[] transparentGrids;

    private List<Renderer> allRenderers = new List<Renderer>();

    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int GridSpacing = Shader.PropertyToID("_GridSpacing");
    private static readonly int MainColor = Shader.PropertyToID("_Color");
    private static readonly Color MainColorDefault = new Color(0.33f, 0.33f, 0.33f, 1f);
    private static readonly Color MainColorHighContrast = new Color(0f, 0f, 0f, 1f);

    private static MaterialPropertyBlock oneBeatPropertyBlock;
    private static MaterialPropertyBlock smallBeatPropertyBlock;
    private static MaterialPropertyBlock detailedBeatPropertyBlock;
    private static MaterialPropertyBlock preciseBeatPropertyBlock;
    private static MaterialPropertyBlock beatColorPropertyBlock;

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
    }

    public void UpdateOffset(float offset)
    {
        Shader.SetGlobalFloat(Offset, offset);
        if (!atsc.IsPlaying)
        {
            GridMeasureSnappingChanged(atsc.gridMeasureSnapping);
        }
    }

    private void GridMeasureSnappingChanged(int snapping)
    {
        float gridSeparation = GetLowestDenominator(snapping);
        if (gridSeparation < 3) gridSeparation = 4;

        oneBeatPropertyBlock.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f);
        foreach (Renderer g in oneBeat) g.SetPropertyBlock(oneBeatPropertyBlock);

        smallBeatPropertyBlock.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (Renderer g in smallBeatSegment) g.SetPropertyBlock(smallBeatPropertyBlock);

        bool useDetailedSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        detailedBeatPropertyBlock.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (Renderer g in detailedBeatSegment)
        {
            g.enabled = useDetailedSegments;
            g.SetPropertyBlock(detailedBeatPropertyBlock);
        }

        bool usePreciseSegments = gridSeparation < snapping;
        gridSeparation *= GetLowestDenominator(Mathf.FloorToInt(snapping / gridSeparation));
        preciseBeatPropertyBlock.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / gridSeparation);
        foreach (Renderer g in preciseBeatSegment)
        {
            g.enabled = usePreciseSegments;
            g.SetPropertyBlock(preciseBeatPropertyBlock);
        }

        UpdateGridColors();
    }

    private void UpdateGridColors(object _ = null)
    {
        float gridAlpha = Settings.Instance.GridTransparency;
        Color newColor = Settings.Instance.HighContrastGrids ? MainColorHighContrast : MainColorDefault;
        newColor.a = 1f - gridAlpha;
        beatColorPropertyBlock.SetColor(MainColor, newColor);
        foreach (Renderer g in transparentGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = !(newColor.a == 1f);
        }
        foreach (Renderer g in opaqueGrids)
        {
            g.SetPropertyBlock(beatColorPropertyBlock);
            g.enabled = newColor.a == 1f;
        }
    }

    private int GetLowestDenominator(int a)
    {
        if (a <= 1) return 2;

        IEnumerable<int> factors = PrimeFactors(a);

        if (factors.Any())
        {
            return factors.Max();
        }
        return a;
    }

    public static List<int> PrimeFactors(int a)
    {
        List<int> retval = new List<int>();
        for (int b = 2; a > 1; b++)
        {
            while (a % b == 0)
            {
                a /= b;
                retval.Add(b);
            }
        }
        return retval;
    }

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= GridMeasureSnappingChanged;
        Settings.ClearSettingNotifications(nameof(Settings.HighContrastGrids));
        Settings.ClearSettingNotifications(nameof(Settings.GridTransparency));
    }
}
