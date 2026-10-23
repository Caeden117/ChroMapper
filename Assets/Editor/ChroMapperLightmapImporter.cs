using System.IO;
using System.IO.Compression;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ScriptedImporter(2, "cmlightmap")]
public sealed class ChroMapperLightmapImporter : ScriptedImporter
{
    private const uint FileSignature = 0x314c4d43;
    private const string TextureKey = "texture";

    public override void OnImportAsset(AssetImportContext context)
    {
        using var file = File.OpenRead(context.assetPath);
        using var compressed = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(compressed);
        if (reader.ReadUInt32() != FileSignature)
        {
            throw new InvalidDataException("Invalid ChroMapper lightmap signature.");
        }
        var width = reader.ReadUInt16();
        var height = reader.ReadUInt16();
        var mipCount = reader.ReadByte();
        var srgb = reader.ReadBoolean();
        var format = srgb ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
        var texture = new Texture2D(width, height, format, mipCount, TextureCreationFlags.MipChain)
        {
            name = Path.GetFileNameWithoutExtension(context.assetPath),
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 2
        };
        for (var mip = 0; mip < mipCount; mip++)
        {
            var count = (width >> mip) * (height >> mip);
            texture.SetPixelData(ChroMapperBakedTextureData.ReadRgbPixels(reader, count), mip);
        }
        // Each stored channel represents a different baked light. Keep the shipped filtering and sRGB decoding.
        texture.Apply(false, true);
        context.AddObjectToAsset(TextureKey, texture);
        context.SetMainObject(texture);
    }
}
