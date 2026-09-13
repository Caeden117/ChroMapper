using UnityEngine;

public class ParametricSliceEndWidthFx : FxTarget
{
    public ParametricSpriteLight SpriteLight;

    public Vector2 ValueBounds = new(0f, 1f);
    public float ValueMultiplier = 1f;

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        if (SpriteLight != null)
            SpriteLight.WidthEnd = Mathf.Clamp(value * ValueMultiplier, ValueBounds.x, ValueBounds.y);
    }
}
