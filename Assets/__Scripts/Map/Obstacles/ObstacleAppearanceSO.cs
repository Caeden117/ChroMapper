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

        obj.SetObstacleOutlineVisibility(Settings.Instance.ObstacleOutlines);

        if (obj.obstacleData._duration < 0 && Settings.Instance.ColorFakeWalls)
        {
            obj.SetColor(negativeDurationColor);
        }
        else
        {
            if (obj.obstacleData._customData != null)
            {
                Vector2 wallSize = obj.obstacleData._customData["_scale"]?.ReadVector2() ?? Vector2.one;
                if (wallSize.x < 0 || wallSize.y < 0 && Settings.Instance.ColorFakeWalls)
                {
                    obj.SetColor(negativeWidthColor);
                }
                else
                {
                    obj.SetColor(defaultObstacleColor);
                }
                if (obj.obstacleData._customData.HasKey("_color"))
                {
                    obj.SetColor(obj.obstacleData._customData["_color"].ReadColor(defaultObstacleColor));
                }
            }
            else if (obj.obstacleData._width < 0 && Settings.Instance.ColorFakeWalls)
            {
                obj.SetColor(negativeWidthColor);
            }
            else
            {
                obj.SetColor(defaultObstacleColor);
            }
        }
    }
}
