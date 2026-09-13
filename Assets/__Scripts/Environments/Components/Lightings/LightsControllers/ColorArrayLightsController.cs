using System;
using UnityEngine;

public class ColorArrayLightsController : MonoBehaviour, IEnvironmentComponentUpdate
{
    [SerializeField] public ColorArrayData[] ColorArrayData = Array.Empty<ColorArrayData>();
    [SerializeField] public Material Material;

    [SerializeField]
    public MaterialPropertyBlockController[] MpbControllers = Array.Empty<MaterialPropertyBlockController>();

    [SerializeField] public string ColorsArrayPropertyName = "_ColorsArray";
    [SerializeField] public string ColorsArrayOffsetPropertyName = "_ColorsArrayOffset";

    private int colorsArrayId;
    private int colorsArrayOffsetId;
    private Vector4[] colorsArray;
    
    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        Start();
    }

    private void Start() => Initialize();

    private bool Initialize()
    {
        colorsArrayId = Shader.PropertyToID(ColorsArrayPropertyName);
        colorsArrayOffsetId = Shader.PropertyToID(ColorsArrayOffsetPropertyName);
        colorsArray = new Vector4[ColorArrayData.Length];

        if (MpbControllers.Length == 0 || Material == null) return false;
        foreach (var data in ColorArrayData) data.OnColorChanged += HandleColorChanged;
        SetColorArrayOffsetToMaterialPropertyBlocks();
        SetColorDataToMaterial();

        return true;
    }

    private void OnDestroy()
    {
        foreach (var data in ColorArrayData) data.OnColorChanged -= HandleColorChanged;
    }

    public void Refresh() => SetColorDataToMaterial();

    private void HandleColorChanged(int index, Color color)
    {
        color = color.linear;
        colorsArray[index] = new Vector4(color.r, color.g, color.b, color.a);
    }

    private void SetColorDataToMaterial() => Material.SetVectorArray(colorsArrayId, colorsArray);

    private void SetColorArrayOffsetToMaterialPropertyBlocks()
    {
        var num = ColorArrayData.Length / MpbControllers.Length;
        for (var i = 0; i < MpbControllers.Length; i++)
        {
            MpbControllers[i].Mpb.SetInt(colorsArrayOffsetId, i * num);
            MpbControllers[i].ApplyChanges();
        }
    }

    public bool ShouldInclude => true;
    public bool ShouldRefresh => true;
}
