using LiteNetLib.Utils;
using UnityEngine;

public class ColorSerializable : INetSerializable
{
    public float R = 0;
    public float G = 0;
    public float B = 0;
    public float A = 1;

    public static implicit operator Color(ColorSerializable serializable)
        => new Color(serializable.R, serializable.G, serializable.B, serializable.A);

    public static implicit operator ColorSerializable(Color color)
        => new ColorSerializable()
        {
            R = color.r,
            G = color.g,
            B = color.b,
            A = color.a
        };

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(R);
        writer.Put(G);
        writer.Put(B);

        if (!Mathf.Approximately(A, 1f))
        {
            writer.Put(A);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        R = reader.GetFloat();
        G = reader.GetFloat();
        B = reader.GetFloat();
        reader.TryGetFloat(out A);
    }
}
