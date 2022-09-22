using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MultiCustomizationLauncher
{
    private static DialogBox dialogBox;
    private static TextBoxComponent nameTextBox;
    private static NestedColorPickerComponent color;
    private static TextBoxComponent serverUrlTextBox;

    public static void OpenMultiCustomization()
    {
        if (dialogBox == null)
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("MultiMapping", "multi.customize")
                .DontDestroyOnClose();

            nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("MultiMapping", "multi.customize.name")
                .WithInitialValue(Settings.Instance.MultiSettings.DisplayName)
                .WithMaximumLength(64);

            color = dialogBox.AddComponent<NestedColorPickerComponent>()
                .WithLabel("MultiMapping", "multi.customize.color")
                .WithInitialValue(Settings.Instance.MultiSettings.GridColor)
                .WithConstantAlpha(1f);

            serverUrlTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("MultiMapping", "multi.customize.server")
                .WithInitialValue(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl);

            dialogBox.AddFooterButton(null, "PersistentUI", "cancel");

            dialogBox.AddFooterButton(ApplyChanges, "MultiMapping", "multi.customize.apply");
        }

        dialogBox.Open();
    }

    private static void ApplyChanges()
    {
        Settings.Instance.MultiSettings.DisplayName = nameTextBox.Value.StripTMPTags();
        Settings.Instance.MultiSettings.GridColor = color.Value;
        Settings.Instance.MultiSettings.ChroMapTogetherServerUrl = serverUrlTextBox.Value;
    }
}
