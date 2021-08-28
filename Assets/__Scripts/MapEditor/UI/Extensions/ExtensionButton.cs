using UnityEngine;
using UnityEngine.Events;

public class ExtensionButton
{
    internal ExtensionButtonUI ButtonUI;

    private Sprite icon;

    private bool interactable = true;

    private UnityAction onClick;

    private string tooltip;

    private bool visible = true;

    public string Tooltip
    {
        get => tooltip;
        set
        {
            tooltip = value;
            if (ButtonUI != null) ButtonUI.Tooltip = tooltip;
        }
    }

    public Sprite Icon
    {
        get => icon;
        set
        {
            icon = value;
            if (ButtonUI != null) ButtonUI.Icon = icon;
        }
    }

    public UnityAction Click
    {
        get => onClick;
        set
        {
            onClick = value;
            if (ButtonUI != null) ButtonUI.SetClickAction(onClick);
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (ButtonUI != null) ButtonUI.Visible = visible;
        }
    }

    public bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;
            if (ButtonUI != null) ButtonUI.Interactable = visible;
        }
    }
}
