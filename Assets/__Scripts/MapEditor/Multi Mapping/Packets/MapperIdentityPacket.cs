using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public class MapperIdentityPacket : INetSerializable
{
    public string Name;
    public int ConnectionId;
    public Color Color;

    public NetPeer? MapperPeer;

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
        Color.r = reader.GetFloat();
        Color.g = reader.GetFloat();
        Color.b = reader.GetFloat();
        Color.a = 1;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ConnectionId);
        writer.Put(Name);
        writer.Put(Color.r);
        writer.Put(Color.g);
        writer.Put(Color.b);
    }
}
