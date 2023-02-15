using UnityEngine;

public class MultiJoinLauncher : MonoBehaviour
{
    [SerializeField] private MultiDirectConnectLauncher multiDirectConnectLauncher;

    private DialogBox dialogBox;
    private TextBoxComponent roomCodeTextBox;

    public void JoinLobby()
    {
        // Temporarily disable United Mapping on dev release channel 
        PersistentUI.Instance.ShowDialogBox("United Mapping is not supported on dev release.",
                    null, PersistentUI.DialogBoxPresetType.Ok);
        return;

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
                .OnClick(() => MultiCustomizationLauncher.OpenMultiCustomization(dialogBox))
                .WithLabel("MultiMapping", "multi.customize");

            dialogBox.OnQuickSubmit(JoinMultiSession);
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
