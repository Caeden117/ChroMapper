using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class MaterialPropertyValuesSetterData : EnvironmentComponentData<MaterialPropertyValuesSetter>
{
    public int MaterialPropertyBlockController;
    public PropertyNameFloatValuePair[] Floats;
    public PropertyNameVectorValuePair[] Vectors;
    public PropertyNameColorValuePair[] Colors;
    public PropertyNameIntValuePair[] Ints;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyValuesSetter comp,
        CreateContainer container)
    {
        comp.MpbController =
            container.GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);
        comp.Floats = Floats
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameFloatValuePair
                {
                    PropertyName = x.PropertyName, Value = x.Value
                })
            .ToArray();
        comp.Vectors = Vectors
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameVectorValuePair
                {
                    PropertyName = x.PropertyName, Vector = x.Vector
                })
            .ToArray();
        comp.Colors = Colors
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameColorValuePair
                {
                    PropertyName = x.PropertyName, Color = x.Color
                })
            .ToArray();
        comp.Ints = Ints
            .Select(x =>
                new MaterialPropertyValuesSetter.PropertyNameIntValuePair
                {
                    PropertyName = x.PropertyName, Value = x.Value
                })
            .ToArray();
    }
}

public class PropertyValuePairBase
{
    public string PropertyName;
}

public class PropertyNameFloatValuePair : PropertyValuePairBase
{
    public float Value;
}

public class PropertyNameIntValuePair : PropertyValuePairBase
{
    public int Value;
}

public class PropertyNameVectorValuePair : PropertyValuePairBase
{
    public Vector4 Vector;
}

public class PropertyNameColorValuePair : PropertyValuePairBase
{
    public Color Color;
}
