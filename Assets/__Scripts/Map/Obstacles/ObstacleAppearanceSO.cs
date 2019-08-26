using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleAppearanceSO", menuName = "Map/Appearance/Obstacle Appearance SO")]
public class ObstacleAppearanceSO : ScriptableObject
{
    [SerializeField] private Color defaultObstacleColor = Color.red;
    [SerializeField] private Color negativeWidthColor = Color.green;
    [SerializeField] private Color negativeDurationColor = Color.yellow;

    public void SetObstacleAppearance(BeatmapObstacleContainer obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            if (obj.obstacleData._duration < 0) mat.SetColor("_ColorTint", negativeDurationColor);
            else if (obj.obstacleData._width <= 0) mat.SetColor("_ColorTint", negativeWidthColor);
            else mat.SetColor("_ColorTint", defaultObstacleColor);
        }
    }
}
