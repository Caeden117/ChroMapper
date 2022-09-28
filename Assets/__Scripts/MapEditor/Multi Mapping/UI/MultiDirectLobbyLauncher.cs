using TMPro;
using UnityEngine;

public class MultiDirectLobbyLauncher : MonoBehaviour
{
    [SerializeField] private AutoSaveController autoSave;
    [SerializeField] private MultiLobbyLauncher multiLobbyLauncher;

    private DialogBox dialogBox;
    private TextBoxComponent portTextBox;
    internal MultiServerNetListener serverNetListener;

    public void StartLobby()
    {
        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null
            || multiLobbyLauncher.serverNetListener != null) return;

        if (dialogBox == null)
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("MultiMapping", "multi.session.host")
                .DontDestroyOnClose();

            dialogBox.AddComponent<TextComponent>()
                .WithInitialValue("Port forwarding is required for clients to connect.");

            portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("MultiMapping", "multi.session.port")
                .WithInitialValue(Settings.Instance.MultiSettings.LastHostedPort)
                .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.MultiSettings.LastHostedPort = port)
                .WithContentType(TMP_InputField.ContentType.IntegerNumber)
                .WithMaximumLength(5);

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenRoomCodeLauncher)
                .WithLabel("MultiMapping", "multi.session.use-code");

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(() => MultiCustomizationLauncher.OpenMultiCustomization(dialogBox))
                .WithLabel("MultiMapping", "multi.customize");

            dialogBox.OnQuickSubmit(StartLobby);
            dialogBox.AddFooterButton(null, "PersistentUI", "cancel");
            dialogBox.AddFooterButton(StartMultiSession, "MultiMapping", "multi.session.host");
        }

        dialogBox.Open();
    }

    private void OpenRoomCodeLauncher()
    {
        dialogBox.Close();
        multiLobbyLauncher.StartLobby();
    }

    private void StartMultiSession()
    {
        autoSave.Save();

        var port = int.Parse(portTextBox.Value);

        serverNetListener = new MultiServerNetListener(Settings.Instance.MultiSettings.LocalIdentity, port, autoSave);
    }

    private void Update() => serverNetListener?.ManualUpdate();

    private void OnDestroy() => serverNetListener?.Dispose();
}
