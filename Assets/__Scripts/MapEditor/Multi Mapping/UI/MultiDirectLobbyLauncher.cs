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
                .WithTitle("Host Session")
                .DontDestroyOnClose();

            dialogBox.AddComponent<TextComponent>()
                .WithInitialValue("Port forwarding is required for clients to connect.");

            portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Port")
                .WithInitialValue(Settings.Instance.MultiSettings.LastHostedPort)
                .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.MultiSettings.LastHostedPort = port)
                .WithContentType(TMP_InputField.ContentType.IntegerNumber)
                .WithMaximumLength(5);

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenRoomCodeLauncher)
                .WithLabel("Use Room Code");

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(MultiCustomizationLauncher.OpenMultiCustomization)
                .WithLabel("Mapper Customization");

            dialogBox.AddFooterButton(null, "Cancel");
            dialogBox.AddFooterButton(StartMultiSession, "Start Session");
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
