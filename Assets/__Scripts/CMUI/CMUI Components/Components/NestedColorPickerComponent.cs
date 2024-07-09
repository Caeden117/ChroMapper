using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NestedColorPickerComponent : CMUIComponentWithLabel<Color>, INavigable
{
    // Cached dialog box
    private static DialogBox nestedDialogBox;
    private static ColorPickerComponent nestedColorPicker;
    private static ButtonComponent submitButton;

    [SerializeField] private Button editButton;
    [SerializeField] private TextMeshProUGUI hexColorText;
    [SerializeField] private Image previewImage;

    private bool useAlpha = true;
    private float constantAlpha = 1f;
    
    [field: SerializeField] public Selectable Selectable { get; set; }

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

            submitButton = nestedDialogBox.AddFooterButton(() => Value = nestedColorPicker.Value, "PersistentUI", "ok");

            nestedDialogBox.OnQuickSubmit(() => OnValueUpdated(nestedColorPicker.Value));
        }
        else
        {
            // We need to refresh the submit and quick submit callbacks here otherwise the cached instance will be
            // editing the previewImage and hexColorText from the first dialog this was created from
            submitButton.OnClick(() => {
                Value = nestedColorPicker.Value;
                nestedDialogBox.Close();
            });
            nestedDialogBox.OnQuickSubmit(() => OnValueUpdated(nestedColorPicker.Value));
            
            nestedColorPicker.Value = Value;
        }

        // TODO: Expose DialogBox container to components
        nestedDialogBox.Open(GetComponentInParent<DialogBox>());
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
