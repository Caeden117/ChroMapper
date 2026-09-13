using System;
using UnityEngine;

public class TextureProcessor3DMaterialSwitchFx : FxTarget
{
    [SerializeField] public Material[] MaterialArray = Array.Empty<Material>();

    [SerializeField] public Vector2 ValueBounds = new(-10f, 10f);

    [SerializeField] public GridElementController[] GridElementControllers = Array.Empty<GridElementController>();
    [SerializeField] public int MaterialIndex;

    private int oldMaterialIndex = -1;

    private void OnValidate() => SetMaterialsIfNeeded();
    private void LateUpdate() => SetMaterialsIfNeeded();

    public override void SetValue(int groupId, int elementId, float value) => SetFloat(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var f = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * (value + 1f));
        MaterialIndex = Mathf.RoundToInt(Mathf.Abs(f));
    }

    private void SetMaterialsIfNeeded()
    {
        MaterialIndex = Mathf.Clamp(MaterialIndex, 0, MaterialArray.Length - 1);
        if (MaterialIndex == oldMaterialIndex) return;

        oldMaterialIndex = MaterialIndex;
        var gridMaterial = MaterialArray[MaterialIndex];
        for (var i = 0; i < GridElementControllers.Length; i++) GridElementControllers[i].SetGridMaterial(gridMaterial);
    }
}
