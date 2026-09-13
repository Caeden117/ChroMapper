using UnityEngine;

public class MaterialPropertyBlockControllerRandomValueSetter : MonoBehaviour
{
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public string PropertyName;
    [SerializeField] public float Min;
    [SerializeField] public float Max = 1000f;

    private MaterialPropertyBlock[] materialPropertyBlocks;
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
        var value = Random.Range(Min, Max);
        if (MpbController == null) return;
        MpbController.Mpb.SetFloat(propertyId, value);
        MpbController.ApplyChanges();
    }
}
