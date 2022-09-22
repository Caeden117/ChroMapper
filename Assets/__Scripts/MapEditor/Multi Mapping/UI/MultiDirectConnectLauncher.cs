using System;
using TMPro;
using UnityEngine;

public class MultiDirectConnectLauncher : MonoBehaviour
{
    [SerializeField] private MultiJoinLauncher multiJoinLauncher;

    private DialogBox dialogBox;
    private TextBoxComponent ipTextBox;
    private TextBoxComponent portTextBox;

    public void OpenDirectConnect()
    {
        if (dialogBox == null)
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("MultiMapping", "multi.session.join")
                .DontDestroyOnClose();

            ipTextBox = dialogBox.AddComponent<TextBoxComponent>()
                    .WithLabel("MultiMapping", "multi.session.ip")
                    .WithInitialValue(Settings.Instance.MultiSettings.LastJoinedIP)
                    .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.MultiSettings.LastJoinedIP = port);

            portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                    .WithLabel("MultiMapping", "multi.session.port")
                    .WithInitialValue(Settings.Instance.MultiSettings.LastJoinedPort)
                    .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.MultiSettings.LastJoinedPort = port)
                    .WithContentType(TMP_InputField.ContentType.IntegerNumber);
            
            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenRoomCodeLauncher)
                .WithLabel("MultiMapping", "multi.session.use-code");

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(MultiCustomizationLauncher.OpenMultiCustomization)
                .WithLabel("MultiMapping", "multi.customize");

            dialogBox.AddFooterButton(null, "PersistentUI", "cancel");
            dialogBox.AddFooterButton(JoinMultiSession, "MultiMapping", "multi.session.join");
        }

        dialogBox.Open();
    }

    private void JoinMultiSession()
    {
        BeatSaberSongContainer.Instance.ConnectToMultiSession(ipTextBox.Value, int.Parse(portTextBox.Value),
            Settings.Instance.MultiSettings.LocalIdentity);
    }

    private void OpenRoomCodeLauncher()
    {
        dialogBox.Close();
        multiJoinLauncher.JoinLobby();
    }
}
