using SimpleJSON;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StrobeColorPickerController : MonoBehaviour, IEditorStateProvider
{
    // A missing strobe payload should place a visible, opaque color instead of silently falling back to black.
    private static readonly Color DefaultStrobeColor = Color.red;

    public static StrobeColorPickerController Instance { get; private set; }

    [SerializeField] private ColorPicker picker;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private Toggle strobeColorToggle;
    [SerializeField] private Toggle pickerTile;
    [SerializeField] private Image pickerTileColor;

    // The persisted state remains authoritative if Unity has not yet resolved the toggle reference.
    public bool IsEnabled => Settings.Instance.PlaceGLSStrobeColor;
    public Color CurrentColor => picker != null ? picker.CurrentColor : LoadColor();
    public static Color LoadedColor => LoadColor();

    // Keep the independent strobe picker state separate from the primary Chroma picker.
    public string StateKey => "strobePicker";

    // Let editor metadata restore strobe state whether this flyout has started yet or not.
    public static void SetLoadedColor(Color color)
    {
        Settings.Instance.GLSStrobeColorR = color.r;
        Settings.Instance.GLSStrobeColorG = color.g;
        Settings.Instance.GLSStrobeColorB = color.b;
        Settings.Instance.GLSStrobeColorA = color.a;
        // Unity picker instances need explicit null checks before restoring the shared strobe color.
        if (Instance != null && Instance.picker != null)
        {
            Instance.picker.CurrentColor = color;
        }
    }

    // Let editor metadata restore the strobe toggle whether this flyout has started yet or not.
    public static void SetLoadedEnabled(bool enabled)
    {
        Settings.Instance.PlaceGLSStrobeColor = enabled;
        // Unity picker instances need explicit null checks before restoring the shared strobe toggle.
        if (Instance != null)
        {
            Instance.SetEnabled(enabled);
        }
    }

    // Keep the hotkey equivalent to the strobe tile: enabling opens the picker and disabling closes it.
    public static void ToggleEnabled()
    {
        if (Settings.Instance.PlaceGLSStrobeColor)
        {
            if (Instance != null)
            {
                Instance.Close();
            }

            SetLoadedEnabled(false);
            return;
        }

        if (Instance != null)
        {
            Instance.Open();
        }
        else
        {
            SetLoadedEnabled(true);
        }
    }

    // Reapply the map-scoped setting after UI initialization so the checkbox always matches placement behavior.
    public static void RefreshLoadedEnabledUi()
    {
        // Unity picker instances need explicit null checks before replaying saved toggle state.
        if (Instance != null)
        {
            Instance.SyncEnabledUi();
        }
    }

    private void Awake()
    {
        Instance = this;
        // This cloned controller would otherwise subscribe to and mutate the global color scheme.
        if (TryGetComponent<CustomColorsUIController>(out var customColors))
        {
            customColors.enabled = false;
        }
    }

    private void Start()
    {
        // Restore this picker's private color before its controls render their first value.
        picker.CurrentColor = LoadColor();
        picker.ONValueChanged.AddListener(HandleColorChanged);

        // The strobe picker is editor-wired, so startup only needs to attach callbacks to serialized controls.
        InitializeStrobeControls();
        ReplaceGlobalColorButtons();
        CreateCloseHitTarget();
        UpdatePickerTile();

        EditorStateService.Register(this);
    }

    private void OnDestroy()
    {
        EditorStateService.Unregister(this);
        if (picker != null)
        {
            picker.ONValueChanged.RemoveListener(HandleColorChanged);
        }

        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public void Open()
    {
        SetEnabled(true);
        // Unity dropdowns need explicit null checks before opening the strobe flyout.
        if (dropdown != null)
        {
            dropdown.ToggleDropdown(true);
        }
    }

    public void Close()
    {
        // Unity dropdowns need explicit null checks before closing the strobe flyout.
        if (dropdown != null)
        {
            dropdown.ToggleDropdown(false);
        }
    }

    public void ToggleFlyout()
    {
        if (dropdown != null && dropdown.Visible)
        {
            Close();
            return;
        }

        Open();
    }

    private void InitializeStrobeControls()
    {
        if (strobeColorToggle != null)
        {
            // Replace the cloned color-type callback with the strobe-color setting callback.
            strobeColorToggle.onValueChanged = new Toggle.ToggleEvent();
            strobeColorToggle.onValueChanged.AddListener(SetEnabled);
        }

        if (pickerTile != null)
        {
            // Tile clicks always open the flyout, even while its backing toggle is already on.
            pickerTile.onValueChanged = new Toggle.ToggleEvent();
            if (pickerTile.GetComponent<StrobeColorPickerTileClickHandler>() == null)
            {
                pickerTile.gameObject.AddComponent<StrobeColorPickerTileClickHandler>();
            }
        }

        // Synchronize the single editor-wired checkbox after assigning callbacks.
        SyncEnabledUi();
    }

    // Let the strobe owner write its current control values directly into map metadata.
    public void CaptureEditorState(JSONObject data)
    {
        var color = new JSONObject();
        color.WriteColor(CurrentColor);
        data["color"] = color;
        data["enabled"] = IsEnabled;
    }

    // Apply restored values only after this flyout has assigned its callbacks and initialized its controls.
    public void LoadEditorState(JSONNode data)
    {
        if (data["color"].IsObject)
        {
            SetLoadedColor(data["color"].ReadColor(DefaultStrobeColor));
        }

        if (data.HasKey("enabled"))
        {
            SetLoadedEnabled(data["enabled"].AsBool);
        }
    }

    private void ReplaceGlobalColorButtons()
    {
        foreach (var globalColorButton in GetComponentsInChildren<CustomColorButton>(true))
        {
            var button = globalColorButton.GetComponent<Button>();
            if (button == null || globalColorButton.image == null)
            {
                continue;
            }

            // Global-color tiles select their displayed source color instead of editing the global scheme.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => picker.CurrentColor = globalColorButton.image.color);
        }
    }

    private void CreateCloseHitTarget()
    {
        // The visual X belongs to the original picker scene control, so this layout-independent hit target gives the strobe flyout its own close action.
        var closeButtonObject = new GameObject("Strobe Color Picker Close Button", typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        closeButtonObject.transform.SetParent(transform, false);
        closeButtonObject.transform.SetAsLastSibling();

        var rectTransform = closeButtonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one;
        rectTransform.anchoredPosition = new Vector2(-16f, -32f);
        rectTransform.sizeDelta = new Vector2(64f, 64f);

        closeButtonObject.GetComponent<LayoutElement>().ignoreLayout = true;
        closeButtonObject.GetComponent<Image>().color = Color.clear;
        closeButtonObject.GetComponent<Button>().onClick.AddListener(Close);
    }

    private void SetEnabled(bool enabled)
    {
        Settings.Instance.PlaceGLSStrobeColor = enabled;
        SyncEnabledUi();
    }

    private void SyncEnabledUi()
    {
        var enabled = Settings.Instance.PlaceGLSStrobeColor;
        // The strobe picker has a single serialized toggle, so routine sync stays local to that control.
        if (strobeColorToggle != null)
        {
            strobeColorToggle.SetIsOnWithoutNotify(enabled);
        }

        UpdatePickerTile();
        // Keep routine editor metadata checkbox synchronization silent to avoid flooding the editor log.
    }

    private void HandleColorChanged(Color color)
    {
        // Save this picker separately so Picker 2.0 never changes its selected color.
        Settings.Instance.GLSStrobeColorR = color.r;
        Settings.Instance.GLSStrobeColorG = color.g;
        Settings.Instance.GLSStrobeColorB = color.b;
        Settings.Instance.GLSStrobeColorA = color.a;
        UpdatePickerTile();
    }

    private void UpdatePickerTile()
    {
        if (pickerTileColor == null)
        {
            return;
        }
        var color = CurrentColor;
        pickerTileColor.color = color.WithAlpha(IsEnabled ? 1f : 0.3f);
    }

    private static Color LoadColor()
    {
        var color = new Color(
            Settings.Instance.GLSStrobeColorR,
            Settings.Instance.GLSStrobeColorG,
            Settings.Instance.GLSStrobeColorB,
            Settings.Instance.GLSStrobeColorA);
        // Legacy empty picker state is transparent black; normalize it to the visible strobe default.
        return color.a <= 0f ? DefaultStrobeColor : color;
    }
}

public class StrobeColorPickerTileClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Unity picker singletons need explicit null checks before routing tile clicks.
            if (StrobeColorPickerController.Instance != null)
            {
                StrobeColorPickerController.Instance.ToggleFlyout();
            }
        }
    }
}
