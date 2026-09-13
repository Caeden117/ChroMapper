using System;
using UnityEngine;

public class ColorSchemeProvider : MonoBehaviour
{
    public ColorSchemeSO ColorScheme;

    public event Action OnColorSchemeChanged;

    private BeatmapRuntimeContext runtimeContext;

    public void Initialize(BeatmapRuntimeContext context)
    {
        if (runtimeContext != null)
            runtimeContext.OnColorSchemeChanged -= HandleColorSchemeChanged;

        runtimeContext = context;
        context.OnColorSchemeChanged += HandleColorSchemeChanged;
        HandleColorSchemeChanged(context.ColorScheme);
    }

    private void HandleColorSchemeChanged(ColorSchemeSO colorScheme)
    {
        ColorScheme = colorScheme;
        OnColorSchemeChanged?.Invoke();
    }

    protected void OnDestroy()
    {
        if (runtimeContext != null)
            runtimeContext.OnColorSchemeChanged -= HandleColorSchemeChanged;

        runtimeContext = null;
    }
}
