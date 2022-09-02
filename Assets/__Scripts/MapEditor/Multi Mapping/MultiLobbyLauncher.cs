using TMPro;
using UnityEngine;

public class MultiLobbyLauncher : MonoBehaviour
{
    [SerializeField] private AutoSaveController autoSave;

    private TextBoxComponent portTextBox;
    private TextBoxComponent nameTextBox;
    private NestedColorPickerComponent color;
    private MultiServerNetListener serverNetListener;

    public void StartLobby()
    {
        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null) return;

        var dialogBox = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Host Multi-Mapping Session");

        portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Port")
                .WithInitialValue(Settings.Instance.LastHostedPort)
                .OnChanged<TextBoxComponent, string>((port) => Settings.Instance.LastHostedPort = port)
                .WithContentType(TMP_InputField.ContentType.IntegerNumber);

        nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithLabel("Display Name")
            .WithInitialValue(Settings.Instance.DisplayName)
            .WithMaximumLength(64)
            .OnChanged<TextBoxComponent, string>((name) => Settings.Instance.DisplayName = name);

        color = dialogBox.AddComponent<NestedColorPickerComponent>()
            .WithLabel("Grid Color")
            .WithInitialValue(Random.ColorHSV(0, 1, 1, 1, 1, 1))
            .WithConstantAlpha(1f);

        dialogBox.AddFooterButton(null, "Cancel");

        dialogBox.AddFooterButton(StartMultiSession, "Start Session");

        dialogBox.Open();
    }

    private void StartMultiSession()
    {
        autoSave.Save();

        var port = int.Parse(portTextBox.Value);

        serverNetListener = new MultiServerNetListener(new MapperIdentityPacket(nameTextBox.Value.StripTMPTags(), 0, color.Value), port, autoSave);
    }

    private void Update() => serverNetListener?.ManualUpdate();

    private void OnDestroy() => serverNetListener?.Dispose();
}
