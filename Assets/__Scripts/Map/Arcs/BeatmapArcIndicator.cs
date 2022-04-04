using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapArcIndicator : BeatmapObjectContainer
{
    public IndicatorType IndicatorType;
    public BeatmapArcContainer ParentArc;

    public override BeatmapObject ObjectData { get; set; }

    public override void UpdateGridPosition()
    {
        var spline = ParentArc.GetComponent<SplineMesh.Spline>();
        var n1 = spline.nodes[0];
        var n2 = spline.nodes[1];

        if (IndicatorType == IndicatorType.Mu)
        {
            transform.localPosition = n1.Direction;
        }
        else if (IndicatorType == IndicatorType.Tmu)
        {
            transform.localPosition = 2 * n2.Position - n2.Direction; // symetric to n2 to make it comprehensible
        }
    }

    public void UpdateMaterials(MaterialPropertyBlock materialPropertyBlock)
    {
        Color color = materialPropertyBlock.GetColor(BeatmapObjectContainer.color);
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color);
        UpdateMaterials();
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat("_Lit", 1);
        MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 0.6f);
        MaterialPropertyBlock.SetFloat("_OpaqueAlpha", 0.6f);

        UpdateMaterials();
    }
}

public enum IndicatorType
{
    Mu,
    Tmu
}
