using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
public abstract class BloomPrePassBackgroundTextureGradient : BloomPrePassNonLightPass
{
    public Color TintColor = Color.white;

    private static readonly int gradientTexId = Shader.PropertyToID("_GradientTex");
    private static readonly int inverseProjectionMatrixId = Shader.PropertyToID("_InverseProjectionMatrix");
    private static readonly int cameraToWorldMatrixId = Shader.PropertyToID("_CameraToWorldMatrix");
    private static readonly int colorId = Shader.PropertyToID("_Color");

    private const string useToneMappingKeyword = "USE_TONE_MAPPING";
    private const string skyGradientShaderName = "ChroMapper/Sky Gradient";
    private const int textureWidth = 128;

    private Texture2D texture;
    private Material material;

    private void InitIfNeeded()
    {
        if (!(material != null) || !(texture != null))
        {
            var skyShader = Shader.Find(skyGradientShaderName);
            if (skyShader == null)
            {
                Debug.LogError($"[BloomPrePass] Shader.Find('{skyGradientShaderName}') returned null. The shader may have a compile error or is missing from the project. Sky gradient will not render.");
                return;
            }

            GameObjectExtensions.DestroySafe(texture);
            GameObjectExtensions.DestroySafe(material);
            texture = new Texture2D(textureWidth, 1, TextureFormat.RGBA32, false, false)
            {
                name = "SkyGradient", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            material = new Material(skyShader);
            material.SetTexture(gradientTexId, texture);
            if (ExecutionTimeType == ExecutionTime.AfterBlur)
                material.EnableKeyword(useToneMappingKeyword);
            else
                material.DisableKeyword(useToneMappingKeyword);
        }
    }

    protected void Start() => UpdateGradientTexture();

    protected void OnDestroy()
    {
        GameObjectExtensions.DestroySafe(texture);
        GameObjectExtensions.DestroySafe(material);
    }

    protected abstract void UpdatePixels(NativeArray<Color32> pixels, int numberOfPixels);

    protected override void OnValidate()
    {
        base.OnValidate();
        if (material != null && texture != null) UpdateGradientTexture();
    }

    public void UpdateGradientTexture()
    {
        InitIfNeeded();
        if (texture == null) return;
        var rawTextureData = texture.GetRawTextureData<Color32>();
        UpdatePixels(rawTextureData, textureWidth);
        texture.Apply();
    }

    public override void Render(RenderTexture dest, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
    {
        InitIfNeeded();
        if (material == null) return;
        material.SetMatrix(inverseProjectionMatrixId, projectionMatrix.inverse);
        material.SetMatrix(cameraToWorldMatrixId, viewMatrix.inverse);
        material.SetColor(colorId, TintColor);
        Graphics.Blit(null, dest, material);
    }
}
