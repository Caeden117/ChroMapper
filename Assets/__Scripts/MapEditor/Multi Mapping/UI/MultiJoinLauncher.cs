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
        => BeatSaberSongContainer.Instance.ConnectToMultiSession(roomCodeTextBox.Value,
            Settings.Instance.MultiSettings.LocalIdentity);

    private void OpenDirectConnect()
    {
        dialogBox.Close();
        multiDirectConnectLauncher.OpenDirectConnect();
    }
}
