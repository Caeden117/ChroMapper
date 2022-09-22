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
                .WithTitle("MultiMapping", "multi.session.join")
                .DontDestroyOnClose();

            roomCodeTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("MultiMapping", "multi.session.code")
                .WithInitialValue(string.Empty);

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenDirectConnect)
                .WithLabel("MultiMapping", "multi.session.use-direct");

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(MultiCustomizationLauncher.OpenMultiCustomization)
                .WithLabel("MultiMapping", "multi.customize");

            dialogBox.AddFooterButton(null, "PersistentUI", "cancel");

            dialogBox.AddFooterButton(JoinMultiSession, "MultiMapping", "multi.session.join");
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
