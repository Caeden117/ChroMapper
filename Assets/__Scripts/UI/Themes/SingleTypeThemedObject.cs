using UnityEngine;
using static ThemeSO;

public abstract class SingleTypeThemedObject : ThemedObject
{
    [SerializeField]
    private ColorType colorType;
    public ColorType ColorType
    {
        get => colorType;
        set
        {
            colorType = value;
            ReapplyCurrent();
        }
    }
}
