using UnityEngine;
using UnityEngine.Events;
public class ExtensionButton
{
    internal ExtensionButtonUI buttonUI;

    private string tooltip;
    public string Tooltip
    {
        get => tooltip;
        set
        {
            tooltip = value;
            if (buttonUI != null) buttonUI.Tooltip = tooltip;
        }
    }

    private Sprite icon;
    public Sprite Icon
    {
        get => icon;
        set
        {
            icon = value;
            if (buttonUI != null) buttonUI.Icon = icon;
        }
    }
    
    private UnityAction onClick;
    public UnityAction OnClick
    {
        get => onClick;
        set
        {
            onClick = value;
            if (buttonUI != null) buttonUI.SetClickAction(onClick);
        }
    }

    private bool visible = true;
    public bool Visible
    {
        get => visible;
        set
        {
            visible = value;
            if (buttonUI != null) buttonUI.Visible = visible;
        }
    }

    private bool interactable = true;
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
