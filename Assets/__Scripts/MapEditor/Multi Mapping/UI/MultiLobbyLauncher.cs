using System;
using TMPro;
using UnityEngine;

public class MultiLobbyLauncher : MonoBehaviour
{
    [SerializeField] private AutoSaveController autoSave;
    [SerializeField] private MultiDirectLobbyLauncher multiDirectLobbyLauncher;

    private DialogBox dialogBox;
    internal MultiServerNetListener serverNetListener;

    private string? roomCode = null;
    private Guid? roomId = null;

    public void StartLobby()
    {
        if (roomCode != null)
        {
            PersistentUI.Instance.ShowDialogBox($"The room code for this session is: {roomCode}",
                null, PersistentUI.DialogBoxPresetType.Ok);

            return;
        }

        if (BeatSaberSongContainer.Instance.MultiMapperConnection != null
            || multiDirectLobbyLauncher.serverNetListener != null) return;

        if (dialogBox == null)
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("Host Session")
                .DontDestroyOnClose();
            
            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(OpenDirectLauncher)
                .WithLabel("Use Direct Connection");

            dialogBox.AddComponent<ButtonComponent>()
                .OnClick(MultiCustomizationLauncher.OpenMultiCustomization)
                .WithLabel("Mapper Customization");

            dialogBox.AddFooterButton(null, "Cancel");
            dialogBox.AddFooterButton(AttemptStartMultiSession, "Start Session");
        }

        dialogBox.Open();
    }

    private void OpenDirectLauncher()
    {
        dialogBox.Close();
        multiDirectLobbyLauncher.StartLobby();
    }

    private void AttemptStartMultiSession() => ChroMapTogetherApi.TryHost(StartMultiSession, OnFail);

    private void StartMultiSession(Guid roomId, int port, string roomCode)
    {
        autoSave.Save();

        this.roomCode = roomCode;
        this.roomId = roomId;

        InvokeRepeating(nameof(KeepAlive), 30, 60);

        PersistentUI.Instance.ShowDialogBox($"The room code for this session is: {roomCode}",
            null, PersistentUI.DialogBoxPresetType.Ok);

        serverNetListener = new MultiServerNetListener(Settings.Instance.MultiSettings.LocalIdentity, port, autoSave);
    }

    private void OnFail(int statusCode, string message)
    {
        PersistentUI.Instance.ShowDialogBox($"Could not host this session (HTTP {statusCode}): {message}",
            null, PersistentUI.DialogBoxPresetType.Ok);
    }

    private void Update() => serverNetListener?.ManualUpdate();

    private void OnDestroy() => serverNetListener?.Dispose();

    private void KeepAlive()
    {
        if (roomId == null) return;

        ChroMapTogetherApi.TryKeepAlive(roomId.Value, (code, message) =>
        {
            PersistentUI.Instance.ShowDialogBox($"Could not keep session alive (HTTP {code}): {message}\n\n" +
                "New users may no longer be able to join your session with the room code.",
                null, PersistentUI.DialogBoxPresetType.Ok);

            roomId = null;
            roomCode = null;
        });
    }
}
