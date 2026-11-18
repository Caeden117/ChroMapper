using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class GLSColorDistributionRowView : MonoBehaviour
{
    [SerializeField] private DropdownComponent targetDropdown;
    [SerializeField] private TextBoxFloatComponent valueInput;
    [SerializeField] private DropdownComponent easingDropdown;
    [SerializeField] private ToggleComponent lightToggle;
    [SerializeField] private ToggleComponent chunkToggle;
    [SerializeField] private ButtonComponent removeButton;

    public DropdownComponent TargetDropdown => targetDropdown;
    public TextBoxFloatComponent ValueInput => valueInput;
    public DropdownComponent EasingDropdown => easingDropdown;
    public ToggleComponent LightToggle => lightToggle;
    public ToggleComponent ChunkToggle => chunkToggle;
    public ButtonComponent RemoveButton => removeButton;

    public event Action<string> OnDistributionChanged;
    public event Action<GLSColorDistributionRowView> OnRemove;

    private static readonly string[] TargetTokens = { "h", "s", "v", "r", "g", "b", "f" };
    private static readonly string[] TargetDisplay = { "Hue (0-360)", "Saturation (0-1)", "Value (0-Inf)", "Red (0-Inf)", "Green (0-Inf)", "Blue (0-Inf)", "Brightness (0-Inf)" };
    private static readonly Dictionary<string, int> TargetTokenToIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        { "h", 0 }, { "s", 1 }, { "v", 2 }, { "r", 3 }, { "g", 4 }, { "b", 5 }, { "f", 6 },
    };
    private static readonly string[] EasingTokens =
    {
        "L", "I^2", "O^2", "IO^2", "I^3", "O^3", "IO^3", "I^4", "O^4", "IO^4", "I^5", "O^5", "IO^5",
        "ISn", "OSn", "IOSn", "IEx", "OEx", "IOEx", "ICr", "OCr", "IOCr", "IBk", "OBk", "IOTBk", "IEl", "OEl", "IOTEl",
        "IBo", "OBo", "IOTBo", "IOBk", "IOEl", "IOBo", "N"
    };
    private static readonly string[] EasingDisplay =
    {
        "Linear", "In Quad", "Out Quad", "InOut Quad", "In Cubic", "Out Cubic", "InOut Cubic",
        "In Quart", "Out Quart", "InOut Quart", "In Quint", "Out Quint", "InOut Quint",
        "In Sine", "Out Sine", "InOut Sine", "In Expo", "Out Expo", "InOut Expo",
        "In Circ", "Out Circ", "InOut Circ", "In Back", "Out Back", "InOut Back",
        "In Elastic", "Out Elastic", "InOut Elastic", "In Bounce", "Out Bounce", "InOut Bounce",
        "InOut Back (BS)", "InOut Elastic (BS)", "InOut Bounce (BS)", "Step"
    };
    private static readonly Dictionary<string, int> EasingTokenToIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        { "L", 0 }, { "I^2", 1 }, { "O^2", 2 }, { "IO^2", 3 }, { "I^3", 4 }, { "O^3", 5 }, { "IO^3", 6 },
        { "I^4", 7 }, { "O^4", 8 }, { "IO^4", 9 }, { "I^5", 10 }, { "O^5", 11 }, { "IO^5", 12 },
        { "ISn", 13 }, { "OSn", 14 }, { "IOSn", 15 }, { "IEx", 16 }, { "OEx", 17 }, { "IOEx", 18 },
        { "ICr", 19 }, { "OCr", 20 }, { "IOCr", 21 }, { "IBk", 22 }, { "OBk", 23 }, { "IOTBk", 24 },
        { "IEl", 25 }, { "OEl", 26 }, { "IOTEl", 27 }, { "IBo", 28 }, { "OBo", 29 }, { "IOTBo", 30 },
        { "IOBk", 31 }, { "IOEl", 32 }, { "IOBo", 33 }, { "N", 34 },
        { "lin", 0 }, { "iq", 1 }, { "oq", 2 }, { "ioq", 3 }, { "ic", 4 }, { "oc", 5 }, { "ioc", 6 },
        { "iqt", 7 }, { "oqt", 8 }, { "ioqt", 9 }, { "iqn", 10 }, { "oqn", 11 }, { "ioqn", 12 },
        { "is", 13 }, { "os", 14 }, { "ios", 15 }, { "ie", 16 }, { "oe", 17 }, { "ioe", 18 },
        { "ib", 22 }, { "ob", 23 }, { "iob", 24 }, { "step", 34 },
        { "easeLinear", 0 }, { "easeInQuad", 1 }, { "easeOutQuad", 2 }, { "easeInOutQuad", 3 },
        { "easeInCubic", 4 }, { "easeOutCubic", 5 }, { "easeInOutCubic", 6 }, { "easeInQuart", 7 }, { "easeOutQuart", 8 }, { "easeInOutQuart", 9 },
        { "easeInQuint", 10 }, { "easeOutQuint", 11 }, { "easeInOutQuint", 12 }, { "easeInSine", 13 }, { "easeOutSine", 14 }, { "easeInOutSine", 15 },
        { "easeInExpo", 16 }, { "easeOutExpo", 17 }, { "easeInOutExpo", 18 }, { "easeInCirc", 19 }, { "easeOutCirc", 20 }, { "easeInOutCirc", 21 },
        { "easeInBack", 22 }, { "easeOutBack", 23 }, { "easeInOutBack", 24 }, { "easeInElastic", 25 }, { "easeOutElastic", 26 }, { "easeInOutElastic", 27 },
        { "easeInBounce", 28 }, { "easeOutBounce", 29 }, { "easeInOutBounce", 30 },
        { "easeBeatSaberInOutBack", 31 }, { "easeBeatSaberInOutElastic", 32 }, { "easeBeatSaberInOutBounce", 33 }, { "easeStep", 34 },
    };
    
    private int targetIdx;
    private float offset;
    private int easingIdx;
    private bool isLight;
    private bool suppressNotify;
    private bool suppressRadio;

    // Hue offsets are stored as turns from 0 to 1, but displayed in degrees. Convert only the hue input.
    private const int HueTargetIndex = 0; // TargetTokens[0] == "h"
    private bool IsHueTarget => targetIdx == HueTargetIndex;
    private float ToDisplayValue(float authored) =>
        IsHueTarget ? (float)Math.Round(authored * 360f, 3) : authored;
    private float FromDisplayValue(float displayed) =>
        IsHueTarget ? displayed / 360f : displayed;

    private void Awake()
    {
        targetDropdown.WithOptions(TargetDisplay);
        easingDropdown.WithOptions(EasingDisplay);

        targetDropdown.OnValueChanged(v =>
        {
            if (suppressNotify) return;
            targetIdx = v;
            valueInput.SetValueWithoutNotify(ToDisplayValue(offset));
            Notify();
        });
        easingDropdown.OnValueChanged(v => { if (suppressNotify) return; easingIdx = v; Notify(); });
        valueInput.OnEndEdit(v => { if (suppressNotify) return; offset = FromDisplayValue(v); Notify(); });
        valueInput.OnValueChanged(v => { if (suppressNotify) return; offset = FromDisplayValue(v); Notify(); });

        lightToggle.OnValueChanged(v =>
        {
            if (suppressNotify || suppressRadio) return;
            if (v) { SetRadio(true); Notify(); }
            else { suppressRadio = true; lightToggle.SetValueWithoutNotify(true); suppressRadio = false; }
        });
        chunkToggle.OnValueChanged(v =>
        {
            if (suppressNotify || suppressRadio) return;
            if (v) { SetRadio(false); Notify(); }
            else { suppressRadio = true; chunkToggle.SetValueWithoutNotify(true); suppressRadio = false; }
        });
        removeButton.OnClick(() => OnRemove?.Invoke(this));
    }

    private void SetRadio(bool light)
    {
        suppressRadio = true;
        lightToggle.SetValueWithoutNotify(light);
        chunkToggle.SetValueWithoutNotify(!light);
        isLight = light;
        suppressRadio = false;
    }

    private void Notify() => OnDistributionChanged?.Invoke(GetDistribution());

    public void SetDistribution(string raw)
    {
        if (!TryParse(raw, out var t, out var o, out var e, out var l))
        {
            t = 0; o = 0f; e = 0; l = false;
        }
        targetIdx = t; offset = o; easingIdx = e; isLight = l;
        suppressNotify = true;
        targetDropdown.SetValueWithoutNotify(Mathf.Clamp(targetIdx, 0, TargetTokens.Length - 1));
        easingDropdown.SetValueWithoutNotify(Mathf.Clamp(easingIdx, 0, EasingTokens.Length - 1));
        valueInput.SetValueWithoutNotify(ToDisplayValue(offset));
        SetRadio(isLight);
        suppressNotify = false;
    }

    public string GetDistribution()
    {
        var target = TargetTokens[Mathf.Clamp(targetIdx, 0, TargetTokens.Length - 1)];
        var easing = EasingTokens[Mathf.Clamp(easingIdx, 0, EasingTokens.Length - 1)];
        var offsetStr = offset.ToString(CultureInfo.InvariantCulture);
        return isLight ? $"{target},{offsetStr},{easing},l" : $"{target},{offsetStr},{easing}";
    }

    private static bool TryParse(string raw, out int targetIdx, out float offset, out int easingIdx, out bool isLight)
    {
        targetIdx = 0; offset = 0f; easingIdx = 0; isLight = false;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var parts = raw.Split(',');
        if (parts.Length < 3) return false;
        var targetPart = parts[0]?.Trim() ?? string.Empty;
        bool found = false;
        foreach (var ch in targetPart)
        {
            if (TargetTokenToIndex.TryGetValue(ch.ToString().ToLowerInvariant(), out var idx)) { targetIdx = idx; found = true; break; }
        }
        if (!found) return false;
        if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out offset)) return false;
        if (float.IsNaN(offset) || float.IsInfinity(offset)) return false;
        if (!EasingTokenToIndex.TryGetValue(parts[2].Trim(), out easingIdx)) easingIdx = 0;
        if (parts.Length >= 4) isLight = string.Equals(parts[3].Trim(), "l", StringComparison.OrdinalIgnoreCase);
        return true;
    }
}
