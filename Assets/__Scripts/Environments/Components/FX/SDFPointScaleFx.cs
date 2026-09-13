using UnityEngine;

public class SDFPointScaleFx : FxTarget
{
    [SerializeField] public SDFPoint ColorPoint;
    [SerializeField] public Vector2 ValueBounds = new(1f, 10f);
    private float startScale;

    protected void Awake() => startScale = 1f;

    public override void SetValue(int groupId, int elementId, float value) => Scale(value);
    public override void TriggerValue(int groupId, int elementId, float value) => Scale(value);

    private void Scale(float value) =>
        ColorPoint.SqrtRadius = startScale * Mathf.Clamp(value, ValueBounds.x, ValueBounds.y);
}
