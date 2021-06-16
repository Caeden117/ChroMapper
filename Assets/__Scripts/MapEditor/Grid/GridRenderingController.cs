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
    [SerializeField] private Renderer[] gridsToDisableForHighContrast;

    private List<Renderer> allRenderers = new List<Renderer>();

    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int GridSpacing = Shader.PropertyToID("_GridSpacing");
    private static readonly int MainAlpha = Shader.PropertyToID("_BaseAlpha");
    private static readonly float MainAlphaDefault = 0.1f;

    private static MaterialPropertyBlock oneBeatPropertyBlock;
    private static MaterialPropertyBlock smallBeatPropertyBlock;
    private static MaterialPropertyBlock detailedBeatPropertyBlock;
    private static MaterialPropertyBlock preciseBeatPropertyBlock;
    private static MaterialPropertyBlock highContrastBeatPropertyBlock;

    private void Awake()
    {
        oneBeatPropertyBlock = new MaterialPropertyBlock();
        smallBeatPropertyBlock = new MaterialPropertyBlock();
        detailedBeatPropertyBlock = new MaterialPropertyBlock();
        preciseBeatPropertyBlock = new MaterialPropertyBlock();
        highContrastBeatPropertyBlock = new MaterialPropertyBlock();

        atsc.GridMeasureSnappingChanged += GridMeasureSnappingChanged;
        allRenderers.AddRange(oneBeat);
        allRenderers.AddRange(smallBeatSegment);
        allRenderers.AddRange(detailedBeatSegment);
        allRenderers.AddRange(preciseBeatSegment);
        Settings.NotifyBySettingName(nameof(Settings.HighContrastGrids), UpdateHighContrastGrids);
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

        UpdateHighContrastGrids();
    }

    private void UpdateHighContrastGrids(object _ = null)
    {
        highContrastBeatPropertyBlock.SetFloat(MainAlpha, Settings.Instance.HighContrastGrids ? 0 : MainAlphaDefault);
        foreach (Renderer g in gridsToDisableForHighContrast) g.SetPropertyBlock(highContrastBeatPropertyBlock);
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
    }
}
