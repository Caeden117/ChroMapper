using UnityEngine;

[ExecuteAlways]
public class MaterialPropertyBlockColorSetter : MonoBehaviour
{
    public MaterialPropertyBlockController Controller;
    public string Property;
    public bool InverseAlpha;
    public bool DisableOnZeroAlpha;
    public bool SendAlphaToProperty;
    public string AlphaProperty;
    public bool MultiplyWithAlpha;

    private int propId;
    private int alphaPropId;
    private bool hasInitialized;

    private Color lastColor;

    private void Awake()
    {
        hasInitialized = true;
        propId = Shader.PropertyToID(Property);
        alphaPropId = Shader.PropertyToID(AlphaProperty);
    }

    private void Start() => SetColor(lastColor);

    public void SetColor(Color color)
    {
        lastColor = color;
        if (!hasInitialized) return;

        if (InverseAlpha) color.a = 1f - color.a;

        if (MultiplyWithAlpha)
        {
            color.r *= color.a;
            color.g *= color.a;
            color.b *= color.a;
        }

        Controller.Mpb.SetColor(propId, color);
        if (SendAlphaToProperty) Controller.Mpb.SetFloat(alphaPropId, color.a);

        Controller.ApplyChanges();
        if (DisableOnZeroAlpha) Controller.ShowRenderer(color.a > 0.01f);
    }
}
