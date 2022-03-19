using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapSliderContainer : BeatmapObjectContainer
{
    private static readonly Color unassignedColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    [SerializeField] private TracksManager manager;
    [FormerlySerializedAs("sliderData")] public BeatmapSlider SliderData;
    [SerializeField] private GameObject indicatorMu;
    [SerializeField] private GameObject indicatorTmu;
    private const float splineYScaleFactor = 3.0f;
    private const float directionZPerturbation = 1e-3f; // a small value to avoid 'look rotation viewing vector is zero'

    private MeshRenderer splineRenderer;
    internal MeshRenderer SplineRenderer { get => splineRenderer; set
        {
            splineRenderer = value;
            splineRenderer.enabled = splineRendererEnabled;
            splineRenderer.SetPropertyBlock(MaterialPropertyBlock);
        } }
    [SerializeField] private List<MeshRenderer> noteRenderer;
    private bool splineRendererEnabled;

    private const float partition = 0.00f;
    public override BeatmapObject ObjectData { get => SliderData; set => SliderData = (BeatmapSlider)value; }

    public static BeatmapSliderContainer SpawnSlider(BeatmapSlider data, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapSliderContainer>();
        container.SliderData = data;
        return container;
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat("_Lit", 1);
        MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 1);
        UpdateMaterials();
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(-1.5f, 0.5f, SliderData.Time * EditorScaleController.EditorScale);
        //RecomputePosition();
        UpdateCollisionGroups();
    }
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;

        //MaterialPropertyBlock.SetVector(shaderScale, scale);
        //UpdateMaterials();
    }

    public void NotifySplineChanged(BeatmapSlider sliderData = null)
    {
        if (sliderData != null)
        {
            SliderData = sliderData;
        }
        if (splineRenderer != null) // since curve has been changed, firstly disable it until it is computed.
        {
            splineRenderer.enabled = false;
        }
        splineRendererEnabled = false;
        var sliderCollection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Slider) as SlidersContainer;
        sliderCollection.RequestForSplineRecompute(this);
    }

    /// <summary>
    /// Defer Bezier Curve based on slider data. Not the official algorithm I guess?
    /// </summary>
    public void RecomputePosition()
    {
        if (SliderData == null) return; // in case that this container has already been recycled when about to compute
        var spline = GetComponent<SplineMesh.Spline>();
        var n1 = spline.nodes[0];
        var n2 = spline.nodes[1];
        n1.Position = new Vector3(SliderData.X, SliderData.Y, 0);
        n2.Position = new Vector3(SliderData.TailX, SliderData.TailY, (SliderData.TailTime - SliderData.Time) * EditorScaleController.EditorScale);
        //var distance = EditorScaleController.EditorScale;
        var d1 = new Vector3(0, splineYScaleFactor * SliderData.HeadControlPointLengthMultiplier, directionZPerturbation);
        var rot1 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(SliderData.Direction).z - 180);
        d1 = rot1 * d1;
        n1.Direction = n1.Position + d1;
        var d2 = new Vector3(0, splineYScaleFactor * SliderData.TailControlPointLengthMultiplier, directionZPerturbation);
        var rot2 = Quaternion.Euler(0, 0, BeatmapNoteContainer.Directionalize(SliderData.TailCutDirection).z - 180);
        d2 = rot2 * d2;
        n2.Direction = n2.Position + d2;
        spline.RefreshCurves();
        splineRendererEnabled = true;
        if (splineRenderer != null) splineRenderer.enabled = true;
        ResetIndicatorsPosition();
    }

    private void ResetIndicatorsPosition()
    {
        var spline = GetComponent<SplineMesh.Spline>();
        var n1 = spline.nodes[0];
        var n2 = spline.nodes[1];
        indicatorMu.transform.localPosition = n1.Direction;
        indicatorTmu.transform.localPosition = 2 * n2.Position - n2.Direction; // symetric to n2 to make it comprehensible
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color);
        UpdateMaterials();
    }

    internal override void UpdateMaterials()
    {
        foreach (var renderer in noteRenderer) renderer.SetPropertyBlock(MaterialPropertyBlock);
        if (SplineRenderer != null)
            SplineRenderer.SetPropertyBlock(MaterialPropertyBlock);
        foreach (var renderer in SelectionRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
    }

    public void SetIndicatorBlocksActive(bool visible)
    {
        foreach (var renderer in noteRenderer) renderer.gameObject.SetActive(visible);
    }

    public void ChangeMu(float modifier)
    {
        SliderData.HeadControlPointLengthMultiplier += modifier;
    }

    public void ChangeTmu(float modifier)
    {
        SliderData.TailControlPointLengthMultiplier += modifier;
    }
}
