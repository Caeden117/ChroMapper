using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleAppearanceSO", menuName = "Map/Appearance/Obstacle Appearance SO")]
public class ObstacleAppearanceSO : ScriptableObject
{
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");

    public Color defaultObstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    [SerializeField] private Color negativeWidthColor = Color.green;
    [SerializeField] private Color negativeDurationColor = Color.yellow;

    public void SetObstacleAppearance(BeatmapObstacleContainer obj, PlatformDescriptor platform = null)
    {
        if (platform != null) defaultObstacleColor = platform.colors.ObstacleColor;
        foreach (Material mat in obj.ModelMaterials)
        {
            if (obj.obstacleData._duration < 0 && Settings.Instance.ColorFakeWalls)
            {
                mat.SetColor(ColorTint, negativeDurationColor);
            }
            else
            {
                if (obj.obstacleData._customData != null)
                {
                    Vector2 wallSize = obj.obstacleData._customData["_scale"]?.ReadVector2() ?? Vector2.one;
                    if (wallSize.x < 0 || wallSize.y < 0 && Settings.Instance.ColorFakeWalls)
                    {
                        mat.SetColor(ColorTint, negativeWidthColor);
                    }
                    else
                    {
                        mat.SetColor(ColorTint, defaultObstacleColor);
                    }
                    if (obj.obstacleData._customData.HasKey("_color"))
                    {
                        mat.SetColor(ColorTint, obj.obstacleData._customData["_color"].ReadColor(defaultObstacleColor));
                    }
                }
                else if (obj.obstacleData._width < 0 && Settings.Instance.ColorFakeWalls)
                {
                    mat.SetColor(ColorTint, negativeWidthColor);
                }
                else
                {
                    mat.SetColor(ColorTint, defaultObstacleColor);
                }
            }
        }
    }
}
