using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MultiClientNetListener : MultiNetListener
{
    public MapDataPacket MapData { get; private set; } = null;

    public MultiClientNetListener(string ip, int port, MapperIdentityPacket identity) : base()
    {
        NetManager.Start();

        var identityWriter = new NetDataWriter();
        identityWriter.Put(identity);

        NetManager.Connect(ip, port, identityWriter);

        RegisterPacketHandler(PacketId.SendZip, OnZipData);
    }

    public MultiClientNetListener(string roomCode, MapperIdentityPacket identity) : base()
    {
        NetManager.Start();

        var identityWriter = new NetDataWriter();
        identityWriter.Put(roomCode);
        identityWriter.Put(identity);
        
        var serverUri = new Uri(Settings.Instance.MultiSettings.ChroMapTogetherServerUrl);
        var domain = serverUri.Host;

        Debug.Log($"Attempting to contact ChroMapTogether server at {domain}:6969...");

        NetManager.Connect(domain, 6969, identityWriter);

        RegisterPacketHandler(PacketId.SendZip, OnZipData);
    }

    public void OnZipData(MultiNetListener _, MapperIdentityPacket __, NetDataReader reader)
        => MapData = reader.Get<MapDataPacket>();

    // For the client, we just update the host latency (everyone else is updated via MapperLatency packets)
    public override void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        var host = Identities.Find(it => it.ConnectionId == 0);

        if (RemotePlayers.TryGetValue(host, out var remoteHost))
        {
            remoteHost.UpdateLatency(latency);
        }
    }

    public override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        SceneTransitionManager.Instance.CancelLoading(string.Empty);
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");

        base.OnNetworkError(endPoint, socketError);
    }

    // For the client, however, a peer disconnected means the Host has lost connection,
    //   so we should kick our clients back to the song list screen.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (disconnectInfo.Reason == DisconnectReason.ConnectionRejected)
        {
            PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.connection.reject", null,
                PersistentUI.DialogBoxPresetType.Ok);
        }
        else
        {
            PersistentUI.Instance.ShowDialogBox("MultiMapping", "multi.connection.host-lost", null,
                PersistentUI.DialogBoxPresetType.Ok, new object[] { disconnectInfo.Reason });
        }

        SceneTransitionManager.Instance.CancelLoading(string.Empty);
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
    }
}
