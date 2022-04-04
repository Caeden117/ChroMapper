using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapArcContainer : BeatmapObjectContainer
{
    private static readonly Color unassignedColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    [SerializeField] private TracksManager manager;
    [FormerlySerializedAs("arcData")] public BeatmapArc ArcData;
    [SerializeField] private GameObject indicatorMu;
    [SerializeField] private GameObject indicatorTmu;
    private const float splineYScaleFactor = 3.0f;
    private const float directionZPerturbation = 1e-3f; // a small value to avoid 'look rotation viewing vector is zero'
    internal static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");
    internal const float arcEmissionIntensity = 6;

    private MaterialPropertyBlock indicatorMaterialPropertyBlock;

    private MeshRenderer splineRenderer;
    internal MeshRenderer SplineRenderer { get => splineRenderer; set
        {
            splineRenderer = value;
            splineRenderer.enabled = splineRendererEnabled;
            splineRenderer.SetPropertyBlock(MaterialPropertyBlock);
        } }
    [SerializeField] private List<GameObject> indicators;
    private bool splineRendererEnabled;

    private const float partition = 0.00f;
    public override BeatmapObject ObjectData { get => ArcData; set => ArcData = (BeatmapArc)value; }

    public static BeatmapArcContainer SpawnArc(BeatmapArc data, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapArcContainer>();
        container.ArcData = data;
        return container;
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat("_Lit", 1);
        MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 1f);
        foreach (var gameObject in indicators) gameObject.GetComponent<BeatmapArcIndicator>().Setup();

        UpdateMaterials();
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(-1.5f, 0.5f, ArcData.Time * EditorScaleController.EditorScale);
        //RecomputePosition();
        foreach (var gameObject in indicators) gameObject.GetComponent<BeatmapArcIndicator>().UpdateGridPosition();
        UpdateCollisionGroups();
    }
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;

        //MaterialPropertyBlock.SetVector(shaderScale, scale);
        //UpdateMaterials();
    }

    public void NotifySplineChanged(BeatmapArc arcData = null)
    {
        if (arcData != null)
        {
            ArcData = arcData;
        }
        if (splineRenderer != null) // since curve has been changed, firstly disable it until it is computed.
        {
            splineRenderer.enabled = false;
        }
        splineRendererEnabled = false;
        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Arc) as ArcsContainer;
        arcCollection.RequestForSplineRecompute(this);
    }

    /// <summary>
    /// Defer Bezier Curve based on arc data. Not the official algorithm I guess?
    /// </summary>
    public void RecomputePosition()
    {
        if (ArcData == null) return; // in case that this container has already been recycled when about to compute
        var spline = GetComponent<SplineMesh.Spline>();
        var n1 = spline.nodes[0];
        var n2 = spline.nodes[1];
        n1.Position = new Vector3(ArcData.X, ArcData.Y, 0);
        n2.Position = new Vector3(ArcData.TailX, ArcData.TailY, (ArcData.TailTime - ArcData.Time) * EditorScaleController.EditorScale);
        //var distance = EditorScaleController.EditorScale;
        var d1 = new Vector3(0, splineYScaleFactor * ArcData.HeadControlPointLengthMultiplier, directionZPerturbation);
        var rot1 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(ArcData.Direction).z - 180);
        d1 = rot1 * d1;
        n1.Direction = n1.Position + d1;
        var d2 = new Vector3(0, splineYScaleFactor * ArcData.TailControlPointLengthMultiplier, directionZPerturbation);
        var rot2 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(ArcData.TailCutDirection).z - 180);
        d2 = rot2 * d2;
        n2.Direction = n2.Position + d2;
        spline.RefreshCurves();
        splineRendererEnabled = true;
        if (splineRenderer != null) splineRenderer.enabled = true;
        ResetIndicatorsPosition();
    }

    private void ResetIndicatorsPosition()
    {
        foreach (var gameObject in indicators) gameObject.GetComponent<BeatmapArcIndicator>().UpdateGridPosition();
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color);
        MaterialPropertyBlock.SetColor(emissionColor, color * arcEmissionIntensity);
        UpdateMaterials();
    }

    internal override void UpdateMaterials()
    {
        foreach (var gameObject in indicators) gameObject.GetComponent<BeatmapArcIndicator>().UpdateMaterials(MaterialPropertyBlock);
        foreach (var gameObject in indicators) gameObject.GetComponent<BeatmapArcIndicator>().OutlineVisible = OutlineVisible;
        if (SplineRenderer != null)
            SplineRenderer.SetPropertyBlock(MaterialPropertyBlock);
        foreach (var renderer in SelectionRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
    }

    public void SetIndicatorBlocksActive(bool visible)
    {
        foreach (var gameObject in indicators) gameObject.SetActive(visible);
    }

    public void ChangeMu(float modifier)
    {
        ArcData.HeadControlPointLengthMultiplier += modifier;
    }

    public void ChangeTmu(float modifier)
    {
        ArcData.TailControlPointLengthMultiplier += modifier;
    }
}
