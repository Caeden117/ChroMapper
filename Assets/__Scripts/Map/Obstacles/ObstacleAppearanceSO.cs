using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ObstacleAppearanceSO", menuName = "Map/Appearance/Obstacle Appearance SO")]
public class ObstacleAppearanceSO : ScriptableObject
{
    [FormerlySerializedAs("defaultObstacleColor")] public Color DefaultObstacleColor = BeatSaberSong.DefaultLeftColor;
    [SerializeField] private Color negativeWidthColor = Color.green;
    [SerializeField] private Color negativeDurationColor = Color.yellow;

    public void SetObstacleAppearance(BeatmapObstacleContainer obj, PlatformDescriptor platform = null)
    {
        if (platform != null) DefaultObstacleColor = platform.Colors.ObstacleColor;

        if (obj.ObstacleData.Duration < 0 && Settings.Instance.ColorFakeWalls)
        {
            obj.SetColor(negativeDurationColor);
        }
        else
        {
            if (obj.ObstacleData.CustomData != null)
            {
                var wallSize = obj.ObstacleData.CustomSize?.ReadVector2() ?? Vector2.one;
                if (wallSize.x < 0 || (wallSize.y < 0 && Settings.Instance.ColorFakeWalls))
                    obj.SetColor(negativeWidthColor);
                else
                    obj.SetColor(DefaultObstacleColor);
                if (obj.ObstacleData.CustomColor)
                    obj.SetColor(obj.ObstacleData.CustomColor.ReadColor(DefaultObstacleColor));
            }
            else if (obj.ObstacleData.Width < 0 && Settings.Instance.ColorFakeWalls)
            {
                obj.SetColor(negativeWidthColor);
            }
            else
            {
                obj.SetColor(DefaultObstacleColor);
            }
        }
    }
}
