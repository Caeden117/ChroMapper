using LiteNetLib.Utils;

public class MapperLatencyPacket : INetSerializable
{
    public int Latency;

    public void Deserialize(NetDataReader reader) => Latency = reader.GetInt();

    public void Serialize(NetDataWriter writer) => writer.Put(Latency);
}
