using System;
using System.Data;
using System.Globalization;
using UnityEngine;

public class TextBoxFloatComponent : TextBoxNumberComponent<float>
{
    protected override bool ParseAndValidate(string res, out float val)
    {
        try
        {
            var dt = new DataTable();
            var r = dt.Compute(res, "");
            res = r.ToString();
        }
        catch (Exception e)
        {
            // ignored
        }

        if (!float.TryParse(res, NumberStyles.Float, CultureInfo.InvariantCulture, out val)) return false;
        val = ValidateValue(val);
        
        return true;
    }

    protected override void OnValueUpdated(float updatedValue)
    {
        if (!InputField.isFocused) InputField.SetTextWithoutNotify(updatedValue.ToString(CultureInfo.InvariantCulture));
    }

    protected override float ValidateValue(float val)
    {
        val = Clamping switch
        {
            NumberClamping.Min => Mathf.Max(MinValue, val),
            NumberClamping.Max => Mathf.Min(MaxValue, val),
            NumberClamping.Clamp => Mathf.Clamp(val, MinValue, MaxValue),
            _ => val
        };

        if (LoopAround) val = Mathf.Repeat(val, LoopThreshold);
        return val;
    }

    protected override float AddValue(float val, float delta)
        => ValidateValue(CMMath.RoundToDecimals(val + (delta * GetPrecisionValue())));
}
