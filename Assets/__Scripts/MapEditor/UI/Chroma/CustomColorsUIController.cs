using System;
using UnityEngine;
using UnityEngine.UI;

//TODO for the love of god please refactor this to not use BeatmapObjectContainerCollection.LoadedContainers.Values
public class CustomColorsUIController : MonoBehaviour
{
    public event Action CustomColorsUpdatedEvent;

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

    // Start is called before the first frame update
    void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += LoadedPlatform;
    }

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;

        SetColorIfNotEqual(ref redNote, platform.colors.RedNoteColor, BeatSaberSong.DEFAULT_LEFTNOTE, BeatSaberSongContainer.Instance.difficultyData.colorLeft);
        SetColorIfNotEqual(ref blueNote, platform.colors.BlueNoteColor, BeatSaberSong.DEFAULT_RIGHTNOTE, BeatSaberSongContainer.Instance.difficultyData.colorRight);
        SetColorIfNotEqual(ref redLight, platform.colors.RedColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.envColorLeft);
        SetColorIfNotEqual(ref blueLight, platform.colors.BlueColor, BeatSaberSong.DEFAULT_RIGHTCOLOR, BeatSaberSongContainer.Instance.difficultyData.envColorRight);
        SetColorIfNotEqual(ref redBoost, platform.colors.RedBoostColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.boostColorLeft);
        SetColorIfNotEqual(ref blueBoost, platform.colors.BlueBoostColor, BeatSaberSong.DEFAULT_RIGHTCOLOR, BeatSaberSongContainer.Instance.difficultyData.boostColorRight);
        SetColorIfNotEqual(ref obstacle, platform.colors.ObstacleColor, BeatSaberSong.DEFAULT_LEFTCOLOR, BeatSaberSongContainer.Instance.difficultyData.obstacleColor);

        platform.colors.RedColor = eventAppearance.RedColor = redLight.color;
        platform.colors.BlueColor = eventAppearance.BlueColor = blueLight.color;
        platform.colors.RedBoostColor = eventAppearance.RedBoostColor = redBoost.color;
        platform.colors.BlueBoostColor = eventAppearance.BlueBoostColor = blueBoost.color;
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
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = redNote.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.difficultyData.colorRight = blueNote.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = redLight.color = eventAppearance.RedColor = platform.colors.RedColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = eventAppearance.BlueColor = platform.colors.BlueColor = blueLight.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.difficultyData.boostColorLeft = redBoost.color = eventAppearance.RedBoostColor = platform.colors.RedBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.difficultyData.boostColorRight = blueBoost.color = eventAppearance.BlueBoostColor = platform.colors.BlueBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = obstacle.color = obstacleAppearance.defaultObstacleColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = null;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = null;
        blueNote.color = platform.defaultColors.BlueNoteColor.WithAlpha(1);
        redNote.color = platform.defaultColors.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetLights()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = null;
        redLight.color = eventAppearance.RedColor = platform.colors.RedColor = platform.defaultColors.RedColor;

        BeatSaberSongContainer.Instance.difficultyData.envColorRight = null;
        blueLight.color = eventAppearance.BlueColor = platform.colors.BlueColor = platform.defaultColors.BlueColor;

        BeatSaberSongContainer.Instance.difficultyData.boostColorLeft = null;
        redBoost.color = eventAppearance.RedBoostColor = platform.colors.RedBoostColor = platform.defaultColors.RedBoostColor;

        BeatSaberSongContainer.Instance.difficultyData.boostColorRight = null;
        blueBoost.color = eventAppearance.BlueBoostColor = platform.colors.BlueBoostColor = platform.defaultColors.BlueBoostColor;

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = null;
        obstacleAppearance.defaultObstacleColor = obstacle.color = platform.defaultColors.ObstacleColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }
}
