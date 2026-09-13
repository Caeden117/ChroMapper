using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MaterialPropertyBlockControllerArrayRandomValueSetter : MonoBehaviour
{
    [SerializeField]
    public MaterialPropertyBlockController[] MpbControllers = Array.Empty<MaterialPropertyBlockController>();

    [SerializeField] public string PropertyName;
    [SerializeField] public Vector3 Min;
    [SerializeField] public Vector3 Max;

    private int propertyId;

    protected void Start()
    {
        RefreshPropertyId();
        ApplyParams();
    }

    protected void OnValidate()
    {
        RefreshPropertyId();
        ApplyParams();
    }

    private void RefreshPropertyId() => propertyId = Shader.PropertyToID(PropertyName);

    private void ApplyParams()
    {
        var vector = new Vector3(
            Random.Range(Min.x, Max.x),
            Random.Range(Min.y, Max.y),
            Random.Range(Min.z, Max.z));
        foreach (var mpbController in MpbControllers)
        {
            if (mpbController == null) continue;
            mpbController.Mpb.SetVector(propertyId, vector);
            mpbController.ApplyChanges();
        }
    }
}
