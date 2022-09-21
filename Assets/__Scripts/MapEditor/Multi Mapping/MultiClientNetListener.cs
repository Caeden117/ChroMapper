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
    public int ClientId { get; private set; } = 0;

    public MapDataPacket? MapData { get; private set; }

    public MultiClientNetListener() : base() { }

    public MultiClientNetListener(string ip, int port, MapperIdentityPacket identity) : base()
    {
        NetManager.Start();
        Connect(ip, port, identity);
    }

    public void Connect(string ip, int port, MapperIdentityPacket identity)
    {
        var identityWriter = new NetDataWriter();
        identityWriter.Put(identity);

        NetManager.Connect(ip, port, identityWriter);
    }

    public override void OnZipData(NetPeer peer, MapDataPacket mapData) => MapData = mapData;

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

    // For the client, however, a peer disconnected means the Host has lost connection, so we should kick our clients back to the song list screen.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        PersistentUI.Instance.ShowDialogBox($"Connection with the host lost: {disconnectInfo.Reason}.\n\nReturning to song list...",
            null, PersistentUI.DialogBoxPresetType.Ok);

        SceneTransitionManager.Instance.CancelLoading(string.Empty);
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
    }
}
