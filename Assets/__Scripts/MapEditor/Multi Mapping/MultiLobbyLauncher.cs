using TMPro;
using UnityEngine;

public class MultiLobbyLauncher : MonoBehaviour
{
    [SerializeField] private AutoSaveController autoSave;

    private TextBoxComponent portTextBox;
    private TextBoxComponent nameTextBox;
    private MultiServerNetListener serverNetListener;

    public void StartLobby()
    {
        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null) return;

        var dialogBox = PersistentUI.Instance.CreateNewDialogBox().WithTitle("Host Multi-Mapping Session");

        portTextBox = dialogBox.AddComponent<TextBoxComponent>()
                .WithLabel("Port")
                .WithInitialValue("6969")
                .WithContentType(TMP_InputField.ContentType.IntegerNumber);

        nameTextBox = dialogBox.AddComponent<TextBoxComponent>()
            .WithLabel("Display Name");

        dialogBox.AddFooterButton(null, "Cancel");

        dialogBox.AddFooterButton(StartMultiSession, "Start Session");

        dialogBox.Open();
    }

    private void StartMultiSession()
    {
        autoSave.Save();

        var port = int.Parse(portTextBox.Value);

        serverNetListener = new MultiServerNetListener(nameTextBox.Value, port, autoSave);
    }

    private void Update() => serverNetListener?.ManualUpdate();
}
