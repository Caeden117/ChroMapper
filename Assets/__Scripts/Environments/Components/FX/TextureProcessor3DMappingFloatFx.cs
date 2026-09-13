using UnityEngine;

public class TextureProcessor3DMappingFloatFx : FxTarget
{
    public enum TextureProcessor3DMapping
    {
        XYZDisplacementScale,
        RadialDisplacementScale,
        MaxScale,
        RotationMultiplier,
        EmissiveModulationStrength
    }

    [SerializeField] public Material Material;
    [SerializeField] public bool UseSlave;
    [SerializeField] public Material SlaveMaterial;

    [SerializeField] public TextureProcessor3DMapping Mapping;

    [SerializeField] public Vector2 ValueBounds = new(-1f, 1f);
    [SerializeField] public bool InvertAxis;
    [SerializeField] public bool InvertAxisSlave;

    private static readonly string[] propertyStrings =
    {
        "_LookupXYZDisplacementScale",
        "_LookupRadialDisplacementScale",
        "_LookupMaxScale",
        "_LookupRotationMultiplier",
        "_LookupEmissiveModulationStrength"
    };

    public override void SetValue(int groupId, int elementId, float value) => SetFloat(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var useSlave = UseSlave && SlaveMaterial != null;
        var direction = InvertAxis ? -1f : 1f;
        var val = Mathf.Lerp(
            ValueBounds.x,
            ValueBounds.y,
            0.5f * ((direction * value) + 1f)
        );
        Material.SetFloat(propertyStrings[(int)Mapping], val);

        if (!useSlave) return;
        var axisDirection = InvertAxisSlave ? -1f : 1f;
        var slaveVal = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * ((axisDirection * value) + 1f));
        SlaveMaterial.SetFloat(propertyStrings[(int)Mapping], slaveVal);
    }
}
