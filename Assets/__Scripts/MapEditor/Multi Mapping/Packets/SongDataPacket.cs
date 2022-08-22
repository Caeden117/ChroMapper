using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

public class SongDataPacket : INetSerializable
{
    public float[] Samples;
    public AudioClip Clip;

    public SongDataPacket() => Samples = new float[] { };

    public SongDataPacket(AudioClip clip)
    {
        Clip = clip;

        Samples = new float[clip.samples];
        clip.GetData(Samples, 0);
    }

    public void Deserialize(NetDataReader reader)
    {
        Samples = reader.GetFloatArray();

        Clip = AudioClip.Create("Remote Clip", Samples.Length, reader.GetInt(), reader.GetInt(), false);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray(Samples);
        writer.Put(Clip.channels);
        writer.Put(Clip.frequency);
    }
}
