using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NestedColorPickerComponent : CMUIComponentWithLabel<Color>
{
    // Cached dialog box
    private static DialogBox nestedDialogBox;
    private static ColorPickerComponent nestedColorPicker;

    [SerializeField] private Button editButton;
    [SerializeField] private TextMeshProUGUI hexColorText;
    [SerializeField] private Image previewImage;

    private bool useAlpha = true;
    private float constantAlpha = 1f;

    /// <summary>
    /// Hides the Alpha slider, and holds the Alpha of the color at the specified value.
    /// </summary>
    /// <param name="alpha">Constant alpha</param>
    /// <returns>Itself, for chaining methods.</returns>
    public NestedColorPickerComponent WithConstantAlpha(float alpha)
    {
        useAlpha = false;
        constantAlpha = alpha;
        return this;
    }

    /// <summary>
    /// Shows the Alpha slider, and allows the Alpha channel to be controllable by the user.
    /// </summary>
    /// <returns>Itself, for chaining methods.</returns>
    public NestedColorPickerComponent WithAlpha()
    {
        useAlpha = true;
        return this;
    }

    private void Start()
    {
        editButton.onClick.AddListener(OnEditButtonClick);
        OnValueUpdated(Value);
    }

    private void OnEditButtonClick()
    {
        if (nestedDialogBox == null)
        {
            nestedDialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .DontDestroyOnClose()
                .WithNoTitle();

            nestedColorPicker = nestedDialogBox.AddComponent<ColorPickerComponent>()
                .WithInitialValue(Value);

            // Determine whether or not we will use constant or changable alpha
            if (useAlpha)
            {
                nestedColorPicker.WithAlpha();
            }
            else
            {
                nestedColorPicker.WithConstantAlpha(constantAlpha);
            }

            var cancel = nestedDialogBox.AddFooterButton(null, "PersistentUI", "cancel");

            var submit = nestedDialogBox.AddFooterButton(() => Value = nestedColorPicker.Value, "PersistentUI", "ok");
        }
        else
        {
            nestedColorPicker.Value = Value;
        }

        nestedDialogBox.Open();
    }

    protected override void OnValueUpdated(Color updatedValue)
    {
        previewImage.color = updatedValue.WithAlpha(useAlpha ? updatedValue.a : constantAlpha);

        hexColorText.text = useAlpha
            ? $"#{ColorUtility.ToHtmlStringRGBA(updatedValue)}"
            : $"#{ColorUtility.ToHtmlStringRGB(updatedValue)}";

        hexColorText.color = HSVUtil.ConvertRgbToHsv(updatedValue).NormalizedV > 0.5f
            ? Color.black
            : Color.white;
    }

    private void OnDestroy() => editButton.onClick.RemoveAllListeners();
}
