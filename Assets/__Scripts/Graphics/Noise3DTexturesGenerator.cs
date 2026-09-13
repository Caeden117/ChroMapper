using System;
using UnityEngine;

[ExecuteAlways]
public sealed class Noise3DTexturesGenerator : MonoBehaviour
{
    [Serializable]
    public struct MaterialTextureParameters
    {
        public string globalPropertyName;
        public MaterialPropertyParameters[] materialProperties;
    }

    [Serializable]
    public struct MaterialPropertyParameters
    {
        public string texturePropertyName;
        public Material material;
    }

    [SerializeField] private MaterialTextureParameters[] data;

    private static Texture3D texture;

    private void Awake()
    {
        foreach (var textureParameters in data)
        {
            if (texture == null)
            {
                var pixels = CreateNoisePixels(16, 16, 16, 6f, 6, 1.8f);
                texture = new Texture3D(16, 16, 16, TextureFormat.Alpha8, false)
                {
                    name = "Cutout Perlin Noise",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
            }

            if (!string.IsNullOrEmpty(textureParameters.globalPropertyName))
                Shader.SetGlobalTexture(textureParameters.globalPropertyName, texture);

            foreach (var materialProperty in textureParameters.materialProperties)
            {
                if (materialProperty.material != null)
                    materialProperty.material.SetTexture(materialProperty.texturePropertyName, texture);
            }
        }
    }

    private static Color32[] CreateNoisePixels(
        int width, int height, int depth, float scale, int repeat, float contrast)
    {
        var pixels = new Color32[width * height * depth];
        for (var z = 0; z < depth; z++)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(
                            (PerlinNoise.Perlin3D(
                                scale * x / width,
                                scale * y / height,
                                scale * z / depth,
                                repeat) - 0.5f) * contrast + 0.5f) * 255f);
                    pixels[x + y * width + z * width * height] = new Color32(value, value, value, value);
                }
            }
        }

        return pixels;
    }
}
