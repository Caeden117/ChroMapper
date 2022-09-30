using LiteNetLib.Utils;
using UnityEngine;

public class QuaternionSerializable : INetSerializable
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;
    public float W = 0;

    public static implicit operator Quaternion(QuaternionSerializable serializable)
        => new Quaternion(serializable.X, serializable.Y, serializable.Z, serializable.W);

    public static implicit operator QuaternionSerializable(Quaternion quaternion)
        => new QuaternionSerializable()
        {
            X = quaternion.x,
            Y = quaternion.y,
            Z = quaternion.z,
            W = quaternion.w
        };

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Z);
        writer.Put(W);
    }

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
        Z = reader.GetFloat();
        W = reader.GetFloat();
    }
}
