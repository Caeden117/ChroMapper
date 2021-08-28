using UnityEngine;
using UnityEngine.Events;

public class ExtensionButton
{
    internal ExtensionButtonUI buttonUI;

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
            if (buttonUI != null) buttonUI.Tooltip = tooltip;
        }
    }

    public Sprite Icon
    {
        get => icon;
        set
        {
            icon = value;
            if (buttonUI != null) buttonUI.Icon = icon;
        }
    }

    public UnityAction Click
    {
        get => onClick;
        set
        {
            onClick = value;
            if (buttonUI != null) buttonUI.SetClickAction(onClick);
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (buttonUI != null) buttonUI.Visible = visible;
        }
    }

    public bool Interactable
    {
        get => interactable;
        set
        {
            interactable = value;
            if (buttonUI != null) buttonUI.Interactable = visible;
        }
    }
}
