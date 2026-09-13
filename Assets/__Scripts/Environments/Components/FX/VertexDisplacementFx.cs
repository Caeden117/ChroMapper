using UnityEngine;

public class VertexDisplacementFx : FxTarget
{
    [SerializeField] public Vector3 DisplacementRanges;
    [SerializeField] public AnimationCurve XAnimationCurve;
    [SerializeField] public AnimationCurve YAnimationCurve;
    [SerializeField] public AnimationCurve ZAnimationCurve;
    [SerializeField] public MaterialPropertyBlockController DisplacementController;
    [SerializeField] public Renderer Renderer;
    [SerializeField] public bool UseTestValue;
    [SerializeField] public float TestFloatValue;

    private static readonly int vertexDisplacementRangeVectorPropertyID =
        Shader.PropertyToID("_DisplacementAxisMultiplier");

    private readonly Bounds bounds = new(Vector3.zero, 1000f * Vector3.one);

    protected void OnValidate()
    {
        if (UseTestValue) SetValue(0, 0, TestFloatValue);
    }

    protected void OnEnable()
    {
        Renderer.bounds = bounds;
        DisplacementController.Mpb.SetVector(
            vertexDisplacementRangeVectorPropertyID,
            CalculateDisplacementVector(0f));
    }

    private Vector4 CalculateDisplacementVector(float value)
    {
        var x = XAnimationCurve.Evaluate(value) * DisplacementRanges.x;
        var y = YAnimationCurve.Evaluate(value) * DisplacementRanges.y;
        var z = ZAnimationCurve.Evaluate(value) * DisplacementRanges.z;
        return new Vector4(x, y, z, 1f);
    }

    public override void SetValue(int groupId, int elementId, float value) => SetValue(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetValue(value);

    private void SetValue(float value)
    {
        DisplacementController.Mpb.SetVector(
            vertexDisplacementRangeVectorPropertyID,
            CalculateDisplacementVector(value));
        DisplacementController.ApplyChanges();
    }
}
