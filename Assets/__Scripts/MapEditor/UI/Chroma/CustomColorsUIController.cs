using UnityEngine;
using UnityEngine.UI;

//TODO for the love of god please refactor this to not use BeatmapObjectContainerCollection.LoadedContainers.Values
public class CustomColorsUIController : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    [Space]
    [SerializeField] private Image redNote;
    [SerializeField] private Image blueNote;
    [SerializeField] private Image redLight;
    [SerializeField] private Image blueLight;
    [SerializeField] private Image redBoost;
    [SerializeField] private Image blueBoost;
    [SerializeField] private Image obstacle;
    [Space]
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventsContainer events;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private PlatformDescriptor platform;
    private Color oldPlatformColorR;
    private Color oldPlatformColorB;

    private Color oldPlatformBoostColorR;
    private Color oldPlatformBoostColorB;

    // Start is called before the first frame update
    void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += LoadedPlatform;
    }

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;
        oldPlatformColorR = platform.RedColor;
        oldPlatformColorB = platform.BlueColor;

        oldPlatformBoostColorR = platform.RedBoostColor;
        oldPlatformBoostColorB = platform.BlueBoostColor;

        SetColorIfNotEqual(ref redNote, platform.RedNoteColor, BeatSaberSong.DEFAULT_LEFTNOTE, BeatSaberSongContainer.Instance.difficultyData.colorLeft);
        SetColorIfNotEqual(ref blueNote, platform.BlueNoteColor, BeatSaberSong.DEFAULT_RIGHTNOTE, BeatSaberSongContainer.Instance.difficultyData.colorRight);
        SetColorIfNotEqual(ref redLight, platform.RedColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.envColorLeft);
        SetColorIfNotEqual(ref blueLight, platform.BlueColor, BeatSaberSong.DEFAULT_RIGHTCOLOR, BeatSaberSongContainer.Instance.difficultyData.envColorRight);
        SetColorIfNotEqual(ref redBoost, platform.RedBoostColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.boostColorLeft);
        SetColorIfNotEqual(ref blueBoost, platform.BlueBoostColor, BeatSaberSong.DEFAULT_RIGHTCOLOR, BeatSaberSongContainer.Instance.difficultyData.boostColorRight);
        SetColorIfNotEqual(ref obstacle, platform.ObstacleColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.obstacleColor);

        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        platform.RedColor = eventAppearance.RedColor = redLight.color;
        platform.BlueColor = eventAppearance.BlueColor = blueLight.color;
        platform.RedBoostColor = redBoost.color;
        platform.BlueBoostColor = blueBoost.color;
        obstacleAppearance.defaultObstacleColor = obstacle.color;
    }

    private void SetColorIfNotEqual(ref Image uiElement, Color platformDefault, Color _default, Color? savedColor)
    {
        if (uiElement.color == _default && uiElement.color != platformDefault) uiElement.color = platformDefault.WithAlpha(1);
        uiElement.color = savedColor ?? uiElement.color;
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
    }

    public void UpdateRedNote()
    {
        redNote.color = picker.CurrentColor.WithAlpha(1);
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = picker.CurrentColor;
        noteAppearance.UpdateColor(picker.CurrentColor, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void UpdateBlueNote()
    {
        blueNote.color = picker.CurrentColor.WithAlpha(1);
        BeatSaberSongContainer.Instance.difficultyData.colorRight = picker.CurrentColor;
        noteAppearance.UpdateColor(redNote.color, picker.CurrentColor);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = picker.CurrentColor;
        eventAppearance.RedColor = platform.RedColor = picker.CurrentColor;
        redLight.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = picker.CurrentColor;
        eventAppearance.BlueColor = platform.BlueColor = picker.CurrentColor;
        blueLight.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.difficultyData.boostColorLeft = picker.CurrentColor;
        platform.RedBoostColor = picker.CurrentColor;
        redBoost.color = picker.CurrentColor.WithAlpha(1);
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.difficultyData.boostColorRight = picker.CurrentColor;
        platform.BlueBoostColor = picker.CurrentColor;
        blueBoost.color = picker.CurrentColor.WithAlpha(1);
    }

    public void UpdateObstacles()
    {
        obstacle.color = picker.CurrentColor.WithAlpha(1);
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = picker.CurrentColor;
        obstacleAppearance.defaultObstacleColor = picker.CurrentColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = null;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = null;
        blueNote.color = platform.BlueNoteColor.WithAlpha(1);
        redNote.color = platform.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void ResetLights()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = null;
        redLight.color = oldPlatformColorR.WithAlpha(1);
        eventAppearance.RedColor = platform.RedColor = oldPlatformColorR;

        BeatSaberSongContainer.Instance.difficultyData.envColorRight = null;
        blueLight.color = oldPlatformColorB;
        eventAppearance.BlueColor = platform.BlueColor = oldPlatformColorB.WithAlpha(1);

        BeatSaberSongContainer.Instance.difficultyData.boostColorLeft = null;
        platform.RedBoostColor = oldPlatformBoostColorR;
        redBoost.color = oldPlatformBoostColorR.WithAlpha(1);

        BeatSaberSongContainer.Instance.difficultyData.boostColorRight = null;
        platform.BlueBoostColor = oldPlatformBoostColorB;
        blueBoost.color = oldPlatformBoostColorB.WithAlpha(1);

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void ResetObstacles()
    {
        obstacle.color = platform.ObstacleColor.WithAlpha(1);
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = null;
        obstacleAppearance.defaultObstacleColor = platform.ObstacleColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
    }
}
