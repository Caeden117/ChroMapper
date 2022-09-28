using LiteNetLib.Utils;
using UnityEngine;

public class ColorSerializable : INetSerializable
{
    public float R = 0;
    public float G = 0;
    public float B = 0;

    public static implicit operator Color(ColorSerializable serializable)
        => new Color(serializable.R, serializable.G, serializable.B, 1f);

    public static implicit operator ColorSerializable(Color color)
        => new ColorSerializable()
        {
            R = color.r,
            G = color.g,
            B = color.b
        };

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(R);
        writer.Put(G);
        writer.Put(B);
    }

    public void Deserialize(NetDataReader reader)
    {
        R = reader.GetFloat();
        G = reader.GetFloat();
        B = reader.GetFloat();
    }
}
