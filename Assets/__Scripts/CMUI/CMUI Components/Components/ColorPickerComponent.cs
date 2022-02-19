using UnityEngine;

public class ColorPickerComponent : CMUIComponent<Color>
{
    [SerializeField] private ColorPicker picker;

    private bool firstUpdate = true;
    private bool useAlpha = true;
    private float constantAlpha = 1f;

    /// <summary>
    /// Hides the Alpha slider, and holds the Alpha of the color at the specified value.
    /// </summary>
    /// <param name="alpha">Constant alpha</param>
    /// <returns>Itself, for chaining methods.</returns>
    public ColorPickerComponent WithConstantAlpha(float alpha)
    {
        picker.Setup.ShowAlpha = useAlpha = false;
        constantAlpha = alpha;
        return this;
    }

    /// <summary>
    /// Shows the Alpha slider, and allows the Alpha channel to be controllable by the user.
    /// </summary>
    /// <returns>Itself, for chaining methods.</returns>
    public ColorPickerComponent WithAlpha()
    {
        picker.Setup.ShowAlpha = useAlpha = true;
        return this;
    }

    private void Start()
    {
        picker.ONValueChanged.AddListener(ColorChanged);

        picker.CurrentColor = useAlpha ? Value : Value.WithAlpha(constantAlpha);
    }

    private void ColorChanged(Color newColor)
    {
        // This *should* be picker.CurrentColor set from Start(). If not, we have a problem.
        if (firstUpdate)
        {
            firstUpdate = false;
            return;
        }

        Value = useAlpha ? newColor : newColor.WithAlpha(constantAlpha);
    }

    private void OnDestroy() => picker.ONValueChanged.RemoveAllListeners();
}
