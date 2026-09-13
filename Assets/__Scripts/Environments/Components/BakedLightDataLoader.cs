using UnityEngine;

[ExecuteAlways]
public class BakedLightDataLoader : MonoBehaviour
{
    private static readonly int lightMap1Id = Shader.PropertyToID("_LightMap1");
    private static readonly int lightMap2Id = Shader.PropertyToID("_LightMap2");

    public LightmapDataSO LightmapData;

    private Texture2D blackTexture;

    protected void Start() => SetTextureDataToShaders();

    private void SetTextureDataToShaders()
    {
        Shader.SetGlobalTexture(
            lightMap1Id,
            LightmapData != null && LightmapData.Lightmap1 != null
                ? LightmapData.Lightmap1
                : GetOrCreateBlackTexture());
        Shader.SetGlobalTexture(
            lightMap2Id,
            LightmapData != null && LightmapData.Lightmap2 != null
                ? LightmapData.Lightmap2
                : GetOrCreateBlackTexture());
    }

    private Texture2D GetOrCreateBlackTexture()
    {
        if (blackTexture == null)
        {
            blackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            blackTexture.SetPixels(new[] { Color.black });
            blackTexture.Apply();
        }

        return blackTexture;
    }
}
