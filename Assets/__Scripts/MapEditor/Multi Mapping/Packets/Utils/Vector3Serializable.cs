using LiteNetLib.Utils;
using UnityEngine;

public class Vector3Serializable : INetSerializable
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;

    public static implicit operator Vector3(Vector3Serializable serializable)
        => new Vector3(serializable.X, serializable.Y, serializable.Z);

    public static implicit operator Vector3Serializable(Vector3 vector)
        => new Vector3Serializable()
        {
            X = vector.x,
            Y = vector.y,
            Z = vector.z
        };

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Z);
    }

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
        Z = reader.GetFloat();
    }
}
