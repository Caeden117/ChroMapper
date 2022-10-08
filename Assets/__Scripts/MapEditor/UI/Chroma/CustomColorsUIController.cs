using System;
using Beatmap.Appearances;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

//TODO for the love of god please refactor this to not use BeatmapObjectContainerCollection.LoadedContainers.Values
public class CustomColorsUIController : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;

    [Space] [SerializeField] private Image redNote;

    [SerializeField] private Image blueNote;
    [SerializeField] private Image redLight;
    [SerializeField] private Image blueLight;
    [SerializeField] private Image redBoost;
    [SerializeField] private Image blueBoost;
    [SerializeField] private Image obstacle;

    [Space] [SerializeField] private NoteAppearanceSO noteAppearance;

    [FormerlySerializedAs("obstacles")] [SerializeField] private ObstacleGridContainer obstacleGrid;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [FormerlySerializedAs("events")] [SerializeField] private EventGridContainer eventGrid;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private PlatformDescriptor platform;

    // Start is called before the first frame update
    private void Start() => LoadInitialMap.PlatformLoadedEvent += LoadedPlatform;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
    public event Action CustomColorsUpdatedEvent;

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;

        SetColorIfNotEqual(ref redNote, platform.Colors.RedNoteColor, BeatSaberSong.DefaultLeftNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorLeft);
        SetColorIfNotEqual(ref blueNote, platform.Colors.BlueNoteColor, BeatSaberSong.DefaultRightNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorRight);
        SetColorIfNotEqual(ref redLight, platform.Colors.RedColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft);
        SetColorIfNotEqual(ref blueLight, platform.Colors.BlueColor, BeatSaberSong.DefaultRightColor,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight);
        SetColorIfNotEqual(ref redBoost, platform.Colors.RedBoostColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft);
        SetColorIfNotEqual(ref blueBoost, platform.Colors.BlueBoostColor, BeatSaberSong.DefaultRightColor,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight);
        SetColorIfNotEqual(ref obstacle, platform.Colors.ObstacleColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor);

        platform.Colors.RedColor = eventAppearance.RedColor = redLight.color;
        platform.Colors.BlueColor = eventAppearance.BlueColor = blueLight.color;
        platform.Colors.RedBoostColor = eventAppearance.RedBoostColor = redBoost.color;
        platform.Colors.BlueBoostColor = eventAppearance.BlueBoostColor = blueBoost.color;
        obstacleAppearance.DefaultObstacleColor = obstacle.color;
    }

    private void SetColorIfNotEqual(ref Image uiElement, Color platformDefault, Color @default, Color? savedColor)
    {
        if (uiElement.color == @default && uiElement.color != platformDefault)
            uiElement.color = platformDefault.WithAlpha(1);
        uiElement.color = savedColor ?? uiElement.color;
    }

    public void UpdateRedNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = redNote.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.color =
            eventAppearance.RedColor = platform.Colors.RedColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.color =
            obstacleAppearance.DefaultObstacleColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = null;
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = null;
        blueNote.color = platform.DefaultColors.BlueNoteColor.WithAlpha(1);
        redNote.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetLights()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = null;
        redLight.color = eventAppearance.RedColor = platform.Colors.RedColor = platform.DefaultColors.RedColor;

        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = null;
        blueLight.color = eventAppearance.BlueColor = platform.Colors.BlueColor = platform.DefaultColors.BlueColor;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = null;
        redBoost.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = platform.DefaultColors.RedBoostColor;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = null;
        blueBoost.color = eventAppearance.BlueBoostColor =
            platform.Colors.BlueBoostColor = platform.DefaultColors.BlueBoostColor;

        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = null;
        obstacleAppearance.DefaultObstacleColor = obstacle.color = platform.DefaultColors.ObstacleColor;
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }
}
