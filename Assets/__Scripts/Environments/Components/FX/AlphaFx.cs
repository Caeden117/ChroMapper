using UnityEngine;

public class AlphaFx : FxTarget
{
    [SerializeField] public MaterialPropertyBlockController[] MpbControllers;
    [SerializeField] public string Property;
    [SerializeField] public Color StaticColor;

    private bool hasInitialized;
    private int propertyId;

    private void Awake() => propertyId = Shader.PropertyToID(Property);

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var color = StaticColor.WithAlpha(value);
        for (var i = 0; i < MpbControllers.Length; i++)
        {
            var mpbController = MpbControllers[i];
            mpbController.Mpb.SetColor(propertyId, color);
            mpbController.ApplyChanges();
        }
    }
}
