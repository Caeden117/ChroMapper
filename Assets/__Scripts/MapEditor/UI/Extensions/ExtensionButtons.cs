using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class ExtensionButtons
{
    private static List<ExtensionButton> buttons = new List<ExtensionButton>();

    public static ExtensionButton AddButton(Sprite icon, string tooltip, UnityAction onClick)
    {
        ExtensionButton extensionButton = new ExtensionButton();
        extensionButton.Tooltip = tooltip;
        extensionButton.Icon = icon;
        extensionButton.OnClick = onClick;
        buttons.Add(extensionButton);
        return extensionButton;
    }

    public static void RemoveButton(ExtensionButton button)
    {
        buttons.Remove(button);
    }

    internal static void ForEachButton(Action<ExtensionButton> callback)
    {
        foreach (ExtensionButton button in buttons)
            callback(button);
    }
}