using Beatmap.Containers;
using Beatmap.Shared;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Obstacle Appearance SO", fileName = "ObstacleAppearanceSO")]
    public class ObstacleAppearanceSO : ScriptableObject
    {
        [SerializeField] public Color DefaultObstacleColor = BeatSaberSong.DefaultLeftColor;
        [SerializeField] private Color negativeWidthColor = Color.green;
        [SerializeField] private Color negativeDurationColor = Color.yellow;

        public void SetObstacleAppearance(ObstacleContainer obj, PlatformDescriptor platform = null)
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
                    var wallSize = new Vector2(obj.ObstacleData.Width, obj.ObstacleData.Height);

                    var customSize = obj.ObstacleData.CustomSize;
                    if (customSize != null && customSize.IsArray)
                    {
                        if (customSize[0].IsNumber) wallSize.x = customSize[0];
                        if (customSize[1].IsNumber) wallSize.y = customSize[1];
                    }

                    if ((wallSize.x < 0 || wallSize.y < 0) && Settings.Instance.ColorFakeWalls)
                        obj.SetColor(negativeWidthColor);
                    else
                        obj.SetColor(DefaultObstacleColor);

                    if (obj.ObstacleData.CustomColor != null)
                        obj.SetColor((Color)obj.ObstacleData.CustomColor);
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
            obj.Animator.SetData(obj.ObstacleData);
        }
    }
}
