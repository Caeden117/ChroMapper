using System;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiJoinLauncher : MonoBehaviour, INatPunchListener
{
    [SerializeField] private MultiDirectConnectLauncher multiDirectConnectLauncher;

    private DialogBox dialogBox;
    private TextBoxComponent roomCodeTextBox;
    private MultiClientNetListener multiClientNetListener;

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
    {
        multiClientNetListener = new MultiClientNetListener();
        multiClientNetListener.NetManager.NatPunchEnabled = true;
        multiClientNetListener.NetManager.NatPunchModule.Init(this);
        multiClientNetListener.NetManager.Start();

        var serverUri = new Uri(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl);
        var domain = serverUri.Host;

        Debug.Log($"Attempting to contact ChroMapTogether server at {domain}:6969...");

        multiClientNetListener.NetManager.NatPunchModule.SendNatIntroduceRequest(domain, 6969, roomCodeTextBox.Value);
    }

    private void JoinMultiSession(string ip, int port)
    {
        Debug.Log($"Attempting to connect to {ip}:{port}...");
        BeatSaberSongContainer.Instance.ConnectToMultiSession(multiClientNetListener,
            ip, port, Settings.Instance.MultiSettings.LocalIdentity);
    }

    private void OpenDirectConnect()
    {
        dialogBox.Close();
        multiDirectConnectLauncher.OpenDirectConnect();
    }

    private void Update()
    {
        multiClientNetListener?.NetManager.PollEvents();
        multiClientNetListener?.NetManager.NatPunchModule.PollEvents();
    }

    public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token) { }

    public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        => JoinMultiSession(targetEndPoint.Address.MapToIPv4().ToString(), targetEndPoint.Port);
}
