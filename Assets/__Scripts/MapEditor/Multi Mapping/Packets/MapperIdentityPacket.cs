using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MapperIdentityPacket : INetSerializable
{
    public string Name;
    public int ConnectionId;
    public ColorSerializable Color;

    public NetPeer? MapperPeer;
    public string? Ip;

    public MapperIdentityPacket() { }

    public MapperIdentityPacket(string name, int id) : this(name, id, Random.ColorHSV(0, 1, 1, 1, 1, 1)) { }

    public MapperIdentityPacket(string name, int id, Color color)
    {
        Name = name;
        ConnectionId = id;
        Color = color;
    }

    public void Deserialize(NetDataReader reader)
    {
        ConnectionId = reader.GetInt();
        Name = reader.GetString();
        Color = reader.Get<ColorSerializable>();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ConnectionId);
        writer.Put(Name);
        writer.Put(Color);
    }
}
