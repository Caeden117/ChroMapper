using LiteNetLib.Utils;

public class MapperPosePacket : INetSerializable
{
    public Vector3Serializable Position;
    public QuaternionSerializable Rotation;
    public float SongPosition;
    public bool IsPlayingSong;
    public float PlayingSongSpeed;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Position);
        writer.Put(Rotation);
        writer.Put(SongPosition);
        writer.Put(IsPlayingSong);
        writer.Put(PlayingSongSpeed);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position = reader.Get<Vector3Serializable>();
        Rotation = reader.Get<QuaternionSerializable>();
        SongPosition = reader.GetFloat();
        IsPlayingSong = reader.GetBool();
        PlayingSongSpeed = reader.GetFloat();
    }
}
