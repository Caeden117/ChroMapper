using UnityEngine;

public class MpbArrayFx : FxTarget
{
    [SerializeField] public MaterialPropertyBlockController[] MpbControllers;
    [SerializeField] public string PropertyName;

    [SerializeField] public Vector2 ValueBounds = new(0f, 1f);
    [SerializeField] public float GranularityMultiplier;

    private int propertyId;

    private void Awake() => propertyId = Shader.PropertyToID(PropertyName);

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        for (var i = 0; i < MpbControllers.Length; i++)
        {
            var mpbController = MpbControllers[i];
            mpbController.Mpb.SetFloat(
                propertyId,
                Mathf.Clamp(value * GranularityMultiplier, ValueBounds.x, ValueBounds.y));
            mpbController.ApplyChanges();
        }
    }
}
