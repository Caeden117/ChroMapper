using System;
using UnityEngine;

public class TextureProcessor3DMappingVectorFx : FxTarget
{
    public enum TextureProcessor3DMapping
    {
        XDisplacement,
        YDisplacement,
        ZDisplacement,
        RadialDisplacement,
        Scale,
        Rotation,
        Emissive
    }

    public enum TextureProcessor3DChannel
    {
        A,
        B,
        C,
        D
    }

    [SerializeField] public Material Material;
    [SerializeField] public bool UseSlave;
    [SerializeField] public Material SlaveMaterial;

    [SerializeField] public TextureProcessor3DMapping Mapping;
    [SerializeField] public TextureProcessor3DChannel Channel;

    [SerializeField] public Vector2 ValueBounds = new(-1f, 1f);
    [SerializeField] public bool InvertAxis;
    [SerializeField] public bool InvertAxisSlave;

    private Vector4 fullVector4;
    private Vector4 fullVector4Slave;

    private static readonly string[] propertyStrings = new string[7]
    {
        "_LookupXDisplacementMapping",
        "_LookupYDisplacementMapping",
        "_LookupZDisplacementMapping",
        "_LookupRadialDisplacementMapping",
        "_LookupScaleMapping",
        "_LookupRotationMapping",
        "_LookupEmissiveMapping"
    };

    public override void SetValue(int groupId, int elementId, float value) => SetFloat(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var direction = InvertAxis ? -1f : 1f;
        var val = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * ((direction * value) + 1f));
        
        var useSlave = UseSlave && SlaveMaterial != null;
        var slaveDirection = InvertAxisSlave ? -1f : 1f;
        var slaveVal = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * ((slaveDirection * value) + 1f));
        
        fullVector4 = Material.GetVector(propertyStrings[(int)Mapping]);
        if (useSlave) fullVector4Slave = SlaveMaterial.GetVector(propertyStrings[(int)Mapping]);

        fullVector4 = Material.GetVector(propertyStrings[(int)Mapping]);
        switch (Channel)
        {
            case TextureProcessor3DChannel.A:
                fullVector4.x = val;
                if (useSlave) fullVector4Slave.x = slaveVal;
                break;
            case TextureProcessor3DChannel.B:
                fullVector4.y = val;
                if (useSlave) fullVector4Slave.y = slaveVal;
                break;
            case TextureProcessor3DChannel.C:
                fullVector4.z = val;
                if (useSlave) fullVector4Slave.z = slaveVal;
                break;
            case TextureProcessor3DChannel.D:
                fullVector4.w = val;
                if (useSlave) fullVector4Slave.w = slaveVal;
                break;
        }

        Material.SetVector(propertyStrings[(int)Mapping], fullVector4);
        if (useSlave) SlaveMaterial.SetVector(propertyStrings[(int)Mapping], fullVector4Slave);
    }
}
