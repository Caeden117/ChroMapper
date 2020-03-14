using System;
using UnityEngine;

public class RoundedFloatSettingsBinder : SettingsBinder
{
    public int DecimalPrecision = 0;
    public int Multiple = 1;

    protected override object SettingsToUIValue(object input)
    {
        if (DecimalPrecision == 0) return Convert.ToSingle(input) * Multiple;
        return (float)Math.Round(Convert.ToSingle(input) * Multiple, DecimalPrecision);
    }

    protected override object UIValueToSettings(object input)
    {
        if (DecimalPrecision == 0) return Mathf.RoundToInt((float)input / Multiple);
        return (float)Math.Round(Convert.ToSingle(input) / Multiple, DecimalPrecision);
    }
}
