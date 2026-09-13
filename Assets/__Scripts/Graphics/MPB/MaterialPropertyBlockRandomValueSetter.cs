using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MaterialPropertyBlockRandomValueSetter : MonoBehaviour
{
    [SerializeField] public Renderer[] Renderers = Array.Empty<Renderer>();
    [SerializeField] public string PropertyName;
    [SerializeField] public float MinValue;
    [SerializeField] public float MaxValue = 1f;

    private MaterialPropertyBlock[] materialPropertyBlocks;
    private int propertyId;

    protected void Start() => ApplyParams();

    protected void OnValidate()
    {
        RefreshPropertyId();
        ApplyParams();
    }

    private void RefreshPropertyId() => propertyId = Shader.PropertyToID(PropertyName);

    private void ApplyParams()
    {
        if (materialPropertyBlocks == null || materialPropertyBlocks.Length != Renderers.Length)
            materialPropertyBlocks = new MaterialPropertyBlock[Renderers.Length];

        for (var i = 0; i < Renderers.Length; i++)
        {
            materialPropertyBlocks[i] ??= new MaterialPropertyBlock();
            materialPropertyBlocks[i].SetFloat(propertyId, Random.Range(MinValue, MaxValue));
            Renderers[i].SetPropertyBlock(materialPropertyBlocks[i]);
        }
    }
}
