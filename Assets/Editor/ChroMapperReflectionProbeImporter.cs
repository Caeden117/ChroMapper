using System.IO;
using System.IO.Compression;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ScriptedImporter(1, "cmprobe")]
public sealed class ChroMapperReflectionProbeImporter : ScriptedImporter
{
    private const uint FileSignature = 0x31524d43;
    private const string CubemapKey = "cubemap";

    public override void OnImportAsset(AssetImportContext context)
    {
        using var file = File.OpenRead(context.assetPath);
        using var compressed = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressed);
        if (reader.ReadUInt32() != FileSignature)
        {
            throw new InvalidDataException("Invalid ChroMapper reflection probe signature.");
        }

        var width = reader.ReadUInt16();
        var mipCount = reader.ReadByte();
        var srgb = reader.ReadBoolean();
        var format = srgb
            ? GraphicsFormat.R8G8B8A8_SRGB
            : GraphicsFormat.R8G8B8A8_UNorm;
        var path = context.assetPath.Replace('\\', '/');
        var texture = new Cubemap(width, format, TextureCreationFlags.MipChain, mipCount)
        {
            name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')],
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 1
        };
        for (var face = 0; face < 6; face++)
        {
            for (var mip = 0; mip < mipCount; mip++)
            {
                var size = width >> mip;
                texture.SetPixelData(ChroMapperBakedTextureData.ReadRgbPixels(reader, size * size), mip, (CubemapFace)face);
            }
        }

        // Packed probe mips contain filtered lighting. Regenerating them from the base image introduces dark surface patterns.
        texture.Apply(false, true);
        context.AddObjectToAsset(CubemapKey, texture);
        context.SetMainObject(texture);
    }
}
