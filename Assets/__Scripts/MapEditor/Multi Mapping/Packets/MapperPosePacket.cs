using LiteNetLib.Utils;
using UnityEngine;

public class MapperPosePacket : INetSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;
    public float SongPosition;
    public bool IsPlayingSong;
    public float PlayingSongSpeed;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Position.x);
        writer.Put(Position.y);
        writer.Put(Position.z);
        writer.Put(Rotation.x);
        writer.Put(Rotation.y);
        writer.Put(Rotation.z);
        writer.Put(Rotation.w);
        writer.Put(SongPosition);
        writer.Put(IsPlayingSong);
        writer.Put(PlayingSongSpeed);
    }

    public void Deserialize(NetDataReader reader)
    {
        Position = new Vector3();
        Position.x = reader.GetFloat();
        Position.y = reader.GetFloat();
        Position.z = reader.GetFloat();
        Rotation = new Quaternion();
        Rotation.x = reader.GetFloat();
        Rotation.y = reader.GetFloat();
        Rotation.z = reader.GetFloat();
        Rotation.w = reader.GetFloat();
        SongPosition = reader.GetFloat();
        IsPlayingSong = reader.GetBool();
        PlayingSongSpeed = reader.GetFloat();
    }
}
