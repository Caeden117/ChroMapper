using TMPro;
using UnityEngine;

public class MultiJoinLauncher : MonoBehaviour
{
    private TextBoxComponent ipTextBox;
    private TextBoxComponent portTextBox;
    private TextBoxComponent nameTextBox;

    public void JoinLobby()
    {
        var dialogBox = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Join Multi-Mapping Session");

        ipTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("IP")
                .WithInitialValue(Settings.Instance.LastConnectedIp)
                .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.LastConnectedIp = port);

        portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Port")
                .WithInitialValue(Settings.Instance.LastConnectedPort)
                .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.LastConnectedPort = port)
                .WithContentType(TMP_InputField.ContentType.IntegerNumber);

        nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithLabel("Display Name")
            .WithInitialValue(Settings.Instance.DisplayName)
            .OnChanged<TextBoxComponent, string>((name) => Settings.Instance.DisplayName = name);

        dialogBox.AddFooterButton(null, "Cancel");

        dialogBox.AddFooterButton(JoinMultiSession, "Join Session");

        dialogBox.Open();
    }

    private void JoinMultiSession()
    {
        BeatSaberSongContainer.Instance.ConnectToMultiSession(ipTextBox.Value, int.Parse(portTextBox.Value), nameTextBox.Value);
    }
}
