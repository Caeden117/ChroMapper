using System.Collections;
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

    private List<Renderer> allRenderers = new List<Renderer>();

    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int GridSpacing = Shader.PropertyToID("_GridSpacing");

    private void Awake()
    {
        atsc.GridMeasureSnappingChanged += GridMeasureSnappingChanged;
        allRenderers.AddRange(oneBeat);
        allRenderers.AddRange(smallBeatSegment);
        allRenderers.AddRange(detailedBeatSegment);
        allRenderers.AddRange(preciseBeatSegment);
    }

    public void UpdateOffset(float offset)
    {
        foreach (Renderer g in allRenderers)
        {
            g.material.SetFloat(Offset, offset);
        }
        if (!atsc.IsPlaying)
        {
            GridMeasureSnappingChanged(atsc.gridMeasureSnapping);
        }
    }

    private void GridMeasureSnappingChanged(int snapping)
    {
        int lowestDenominator = GetLowestDenominator(snapping);

        if (lowestDenominator < 3) lowestDenominator = 4;
        
        foreach (Renderer g in oneBeat)
        {
            g.enabled = true;
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f);
        }

        foreach (Renderer g in smallBeatSegment)
        {
            g.enabled = true;
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / lowestDenominator);
        }

        bool useDetailedSegments = lowestDenominator < snapping;
        foreach (Renderer g in detailedBeatSegment)
        {
            g.enabled = useDetailedSegments;
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / (lowestDenominator * 2f));
        }

        bool usePreciseSegments = (lowestDenominator * 2) < snapping;
        foreach (Renderer g in preciseBeatSegment)
        {
            g.enabled = usePreciseSegments;
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / (lowestDenominator * 4f));
        }
    }

    private int GetLowestDenominator(int a)
    {
        if (a <= 1) return 1;
        IEnumerable<int> factors = Enumerable.Range(1, a - 1).Where(n => a % n == 0);
        if (!Mathf.IsPowerOfTwo(a))
        {
            factors = factors.Where(x => !Mathf.IsPowerOfTwo(x));
        }
        if (factors.Any())
        {
            return factors.Max();
        }
        return a;
    }

    private void OnDestroy()
    {
        atsc.GridMeasureSnappingChanged -= GridMeasureSnappingChanged;
    }
}
