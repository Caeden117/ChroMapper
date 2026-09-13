using UnityEngine;

public class ColliderFx : FxTarget
{
    public Collider Collider;
    public float Value;

    public ColliderRepository Repository;

    private void Start() => Repository.Register(this);

    public override void SetValue(int group, int id, float value) => Value = value;
    public override void TriggerValue(int group, int id, float value) => Value = value;
}
