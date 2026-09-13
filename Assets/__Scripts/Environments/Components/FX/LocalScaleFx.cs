using UnityEngine;

public class LocalScaleFx : FxTarget
{
    public Transform[] TargetTransforms;
    public Vector2 ValueBounds = new(1f, 10f);

    private Vector3 startScale;

    private void Awake()
    {
        if (TargetTransforms.Length > 0) startScale = TargetTransforms[^1].localScale;
    }

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        for (var i = 0; i < TargetTransforms.Length; i++)
            TargetTransforms[i].localScale = startScale * Mathf.Clamp(value, ValueBounds.x, ValueBounds.y);
    }
}
