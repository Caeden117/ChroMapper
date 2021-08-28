using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScaleController : MonoBehaviour
{
    private readonly Dictionary<CanvasScaler, Vector2> scalers = new Dictionary<CanvasScaler, Vector2>();

    private void Start()
    {
        foreach (var scaler in GetComponentsInChildren<CanvasScaler>()) scalers.Add(scaler, scaler.referenceResolution);

        Settings.NotifyBySettingName(nameof(Settings.UIScale), RecalculateScale);

        RecalculateScale(Settings.Instance.UIScale);
    }

    private void RecalculateScale(object obj)
    {
        var scale = Convert.ToSingle(obj);
        foreach (var kvp in scalers) kvp.Key.referenceResolution = kvp.Value * scale;
    }
}
