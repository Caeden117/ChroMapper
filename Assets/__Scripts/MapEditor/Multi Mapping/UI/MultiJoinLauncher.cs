using System;
using TMPro;
using UnityEngine;

public class MultiJoinLauncher : MonoBehaviour
{
    [SerializeField] private MultiDirectConnectLauncher multiDirectConnectLauncher;

    private DialogBox dialogBox;
    private TextBoxComponent roomCodeTextBox;

    public void JoinLobby()
    {
        if (dialogBox == null)
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("Join Session")
                .DontDestroyOnClose();

            roomCodeTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Room Code")
                .WithInitialValue(string.Empty);

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenDirectConnect)
                .WithLabel("Use Direct Connection");                

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(MultiCustomizationLauncher.OpenMultiCustomization)
                .WithLabel("Mapper Customization");

            dialogBox.AddFooterButton(null, "Cancel");

            dialogBox.AddFooterButton(JoinMultiSession, "Join Session");
        }

        dialogBox.Open();
    }

    private void JoinMultiSession()
        => ChroMapTogetherApi.TryRoomCode(roomCodeTextBox.Value, JoinMultiSession, CodeFailed);

    private void JoinMultiSession(string ip, int port)
        => BeatSaberSongContainer.Instance.ConnectToMultiSession(ip, port, Settings.Instance.MultiSettings.LocalIdentity);

    private void CodeFailed(int responseCode, string errorMsg)
        => PersistentUI.Instance.ShowDialogBox($"Could not connect to room code (HTTP {responseCode}): {errorMsg}",
            null, PersistentUI.DialogBoxPresetType.Ok);

    private void OpenDirectConnect()
    {
        dialogBox.Close();
        multiDirectConnectLauncher.OpenDirectConnect();
    }
}
