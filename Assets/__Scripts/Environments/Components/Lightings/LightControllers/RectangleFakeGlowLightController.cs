using UnityEngine;

public class RectangleFakeGlowLightController : LightController
{
    public MaterialPropertyBlockController MpbController;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public Vector2 Size = Vector2.one;
    public float EdgeSize = 0.1f;

    private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");

    public override bool IsPhysical => true;
    protected override bool Initialize()
    {
        if (MpbController == null) MpbController = GetComponent<MaterialPropertyBlockController>();
        return MpbController != null;
    }

    public override void SetColor(Color color)
    {
        Color = color;
        if (!HasInitialized) return;

        color.a *= AlphaMultiplier;
        if (color.a < MinAlpha) color.a = MinAlpha;

        var size = new Vector4(Size.x * 0.5f, Size.y * 0.5f, 1f, EdgeSize * 0.5f);
        transform.localScale = size;
        MpbController.Mpb.SetColor(ColorId, color);
        MpbController.Mpb.SetVector(sizeParamsId, size);
        MpbController.ApplyChanges();
    }
}
