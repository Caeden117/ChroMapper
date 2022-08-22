using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

public class MapDataPacket : INetSerializable
{
    public byte[] ZipBytes;
    public string Characteristic;
    public string Diff;

    public MapDataPacket() { }

    public MapDataPacket(byte[] zipBytes, string characteristic, string diff)
    {
        ZipBytes = zipBytes;
        Characteristic = characteristic;
        Diff = diff;
    }

    public void Deserialize(NetDataReader reader)
    {
        Characteristic = reader.GetString();
        Diff = reader.GetString();
        ZipBytes = reader.GetBytesWithLength();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Characteristic);
        writer.Put(Diff);
        writer.PutBytesWithLength(ZipBytes);
    }
}
