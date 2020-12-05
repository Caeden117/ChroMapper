using UnityEngine;

public abstract class ThemedObject : MonoBehaviour
{
    private ThemeSO lastTheme;

    void Start()
    {
        SetTheme(ThemeManager.Instance.currentTheme);
    }

    public void SetTheme(ThemeSO theme)
    {
        lastTheme = theme;
        HandleTheme(theme);
    }
    public void ReapplyCurrent()
    {
        HandleTheme(lastTheme);
    }
    protected abstract void HandleTheme(ThemeSO theme);
}
