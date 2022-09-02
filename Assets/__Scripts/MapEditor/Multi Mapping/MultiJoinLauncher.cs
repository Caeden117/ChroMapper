using TMPro;
using UnityEngine;

public class MultiJoinLauncher : MonoBehaviour
{
    private TextBoxComponent ipTextBox;
    private TextBoxComponent portTextBox;
    private TextBoxComponent nameTextBox;
    private NestedColorPickerComponent color;

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
            .WithMaximumLength(64)
            .OnChanged<TextBoxComponent, string>((name) => Settings.Instance.DisplayName = name);
        
        color = dialogBox.AddComponent<NestedColorPickerComponent>()
            .WithLabel("Grid Color")
            .WithInitialValue(Random.ColorHSV(0, 1, 1, 1, 1, 1));

        dialogBox.AddFooterButton(null, "Cancel");

        dialogBox.AddFooterButton(JoinMultiSession, "Join Session");

        dialogBox.Open();
    }

    private void JoinMultiSession()
    {
        BeatSaberSongContainer.Instance.ConnectToMultiSession(ipTextBox.Value, int.Parse(portTextBox.Value), new MapperIdentityPacket(nameTextBox.Value.StripTMPTags(), 0, color.Value));
    }
}
