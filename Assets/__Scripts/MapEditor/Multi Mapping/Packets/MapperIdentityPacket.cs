using LiteNetLib;
using LiteNetLib.Utils;

public class MapperIdentityPacket : INetSerializable
{
    public string Name;
    public int ConnectionId;

    public NetPeer? MapperPeer;

    public MapperIdentityPacket() { }

    public MapperIdentityPacket(string name, int id)
    {
        Name = name;
        ConnectionId = id;
    }

    public void Deserialize(NetDataReader reader)
    {
        ConnectionId = reader.GetInt();
        Name = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ConnectionId);
        writer.Put(Name);
    }
}
