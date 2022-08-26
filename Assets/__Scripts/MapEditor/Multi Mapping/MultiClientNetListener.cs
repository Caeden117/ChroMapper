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

    public MultiClientNetListener(string ip, int port, string name) : base()
    {
        NetManager.Start();

        var identityWriter = new NetDataWriter();
        identityWriter.Put(new MapperIdentityPacket(name, 0));

        NetManager.Connect(ip, port, identityWriter);
    }

    public override void OnZipData(NetPeer peer, MapDataPacket mapData)
    {
        MapData = mapData;
    }

    public override void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        SceneTransitionManager.Instance.CancelInvoke();
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");

        base.OnNetworkError(endPoint, socketError);
    }

    // For the client, however, a peer disconnected means the Host has lost connection, so we should kick our clients back to the song list screen.
    public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        PersistentUI.Instance.ShowDialogBox($"Connection with the host lost: {disconnectInfo.Reason}.\n\nReturning to song list...",
            null, PersistentUI.DialogBoxPresetType.Ok);

        SceneTransitionManager.Instance.CancelInvoke();
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
    }
}
