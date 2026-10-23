using System.IO;
using UnityEngine;

internal static class ChroMapperBakedTextureData
{
    internal static Color32[] ReadRgbPixels(BinaryReader reader, int count)
    {
        var pixels = new Color32[count];
        for (var index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255);
        }
        return pixels;
    }
}
