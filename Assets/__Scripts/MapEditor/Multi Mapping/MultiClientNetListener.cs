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

        var host = NetManager.Connect(ip, port, identityWriter);
    }

    public override void OnZipData(NetPeer peer, MapDataPacket mapData)
    {
        MapData = mapData;
    }
}
