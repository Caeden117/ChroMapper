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
                .WithInitialValue("127.0.0.1");

        portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Port")
                .WithInitialValue("6969")
                .WithContentType(TMP_InputField.ContentType.IntegerNumber);

        nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithLabel("Display Name");

        dialogBox.AddFooterButton(null, "Cancel");

        dialogBox.AddFooterButton(JoinMultiSession, "Join Session");

        dialogBox.Open();
    }

    private void JoinMultiSession()
    {
        BeatSaberSongContainer.Instance.ConnectToMultiSession(ipTextBox.Value, int.Parse(portTextBox.Value), nameTextBox.Value);
    }
}
