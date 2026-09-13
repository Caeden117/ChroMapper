using System;
using UnityEngine;

[ExecuteAlways]
public class MaterialPropertyValuesSetter : MonoBehaviour
{
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public PropertyNameFloatValuePair[] Floats = Array.Empty<PropertyNameFloatValuePair>();
    [SerializeField] public PropertyNameVectorValuePair[] Vectors = Array.Empty<PropertyNameVectorValuePair>();
    [SerializeField] public PropertyNameColorValuePair[] Colors = Array.Empty<PropertyNameColorValuePair>();
    [SerializeField] public PropertyNameIntValuePair[] Ints = Array.Empty<PropertyNameIntValuePair>();

    protected void Start()
    {
        RefreshPropertyIds();
        ApplyParams();
    }

    protected void OnValidate()
    {
        if (MpbController == null) MpbController = GetComponent<MaterialPropertyBlockController>();
        RefreshPropertyIds();
        ApplyParams();
    }

    private void RefreshPropertyIds()
    {
        foreach (var pair in Floats) pair.RefreshPropertyId();
        foreach (var pair in Vectors) pair.RefreshPropertyId();
        foreach (var pair in Colors) pair.RefreshPropertyId();
        foreach (var pair in Ints) pair.RefreshPropertyId();
    }

    private void ApplyParams()
    {
        foreach (var pair in Floats) MpbController.Mpb.SetFloat(pair.PropertyId, pair.Value);
        foreach (var pair in Vectors) MpbController.Mpb.SetVector(pair.PropertyId, pair.Vector);
        foreach (var pair in Colors) MpbController.Mpb.SetVector(pair.PropertyId, pair.Color);
        foreach (var pair in Ints) MpbController.Mpb.SetInt(pair.PropertyId, pair.Value);
        MpbController.ApplyChanges();
    }

    [Serializable]
    public abstract class PropertyValuePairBase
    {
        [SerializeField] public string PropertyName;
        public int PropertyId { get; private set; }
        protected PropertyValuePairBase() => RefreshPropertyId();
        public void RefreshPropertyId() => PropertyId = Shader.PropertyToID(PropertyName);
    }

    [Serializable]
    public class PropertyNameFloatValuePair : PropertyValuePairBase
    {
        public float Value;
    }

    [Serializable]
    public class PropertyNameIntValuePair : PropertyValuePairBase
    {
        public int Value;
    }

    [Serializable]
    public class PropertyNameVectorValuePair : PropertyValuePairBase
    {
        public Vector4 Vector;
    }

    [Serializable]
    public class PropertyNameColorValuePair : PropertyValuePairBase
    {
        public Color Color;
    }
}
