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
                .WithTitle("Mapper Customization")
                .DontDestroyOnClose();

            nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Display Name")
                .WithInitialValue(Settings.Instance.MultiSettings.DisplayName)
                .WithMaximumLength(64);

            color = dialogBox.AddComponent<NestedColorPickerComponent>()
                .WithLabel("Grid Color")
                .WithInitialValue(Settings.Instance.MultiSettings.GridColor)
                .WithConstantAlpha(1f);

            serverUrlTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("ChroMapTogether Server URL")
                .WithInitialValue(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl);

            dialogBox.AddFooterButton(null, "Cancel");

            dialogBox.AddFooterButton(ApplyChanges, "Apply Changes");
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
