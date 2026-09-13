using UnityEngine;

public class TextureProcessor3DPresetFx : FxTarget
{
    [SerializeField] public TextureProcessor3D TextureProcessor3D;
    
    [SerializeField] public Vector2 ValueBounds = new(0f, 1f);

    public override void SetValue(int groupId, int elementId, float value) => SetFloat(value);
    public override void TriggerValue(int groupId, int elementId, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var f = Mathf.Lerp(ValueBounds.x, ValueBounds.y, 0.5f * (value + 1f));
        TextureProcessor3D.ActivePresetIndex = Mathf.RoundToInt(Mathf.Abs(f));
    }
}
