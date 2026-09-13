using UnityEngine;

public class MpbStepFx : FxTarget
{
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public string PropertyName;

    [SerializeField] public float StepFactor;
    [SerializeField] public float StepSize;

    private int propertyId;

    private void Awake() => propertyId = Shader.PropertyToID(PropertyName);

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        MpbController.Mpb.SetFloat(propertyId, Mathf.Floor(value / StepFactor) * StepSize);
        MpbController.ApplyChanges();
    }
}
