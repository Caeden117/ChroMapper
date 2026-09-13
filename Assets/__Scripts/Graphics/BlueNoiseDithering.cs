using UnityEngine;

public sealed class BlueNoiseDithering : ScriptableObject
{
    [SerializeField] private Texture2D noiseTexture;

    private static readonly int noiseParamsId = Shader.PropertyToID("_GlobalBlueNoiseParams");
    private static readonly int globalNoiseTextureId = Shader.PropertyToID("_GlobalBlueNoiseTex");

    public void SetBlueNoiseShaderParams(int cameraPixelWidth, int cameraPixelHeight)
    {
        Shader.SetGlobalVector(
            noiseParamsId,
            new Vector4(
                cameraPixelWidth / (float)noiseTexture.width,
                cameraPixelHeight / (float)noiseTexture.height,
                0f,
                0f));
        Shader.SetGlobalTexture(globalNoiseTextureId, noiseTexture);
    }
}
