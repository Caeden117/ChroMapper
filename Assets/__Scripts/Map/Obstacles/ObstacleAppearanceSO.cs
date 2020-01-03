using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleAppearanceSO", menuName = "Map/Appearance/Obstacle Appearance SO")]
public class ObstacleAppearanceSO : ScriptableObject
{
    public Color defaultObstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
    [SerializeField] private Color negativeWidthColor = Color.green;
    [SerializeField] private Color negativeDurationColor = Color.yellow;
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");

    public void SetObstacleAppearance(BeatmapObstacleContainer obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            if (obj.obstacleData._duration < 0) mat.SetColor(ColorTint, negativeDurationColor);
            else if (obj.obstacleData._width <= 0) mat.SetColor(ColorTint, negativeWidthColor);
            else mat.SetColor(ColorTint, defaultObstacleColor);
        }
    }
}
