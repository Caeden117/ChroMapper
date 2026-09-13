using UnityEngine;

[CreateAssetMenu(fileName = "LightmapData", menuName = "Environment/Lightmap Data")]
public class LightmapDataSO : ScriptableObject
{
    public Texture2D Lightmap1;
    public Texture2D Lightmap2;
}