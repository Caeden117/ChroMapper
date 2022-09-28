using System;
using UnityEngine;
using UnityEngine.UI;

public class CustomColorsUIController : MonoBehaviour
{
    public event Action CustomColorsUpdatedEvent;

    [SerializeField] private ColorPicker picker;

    [Space] [SerializeField] private Image redNote;

    [SerializeField] private Image blueNote;
    [SerializeField] private Image redLight;
    [SerializeField] private Image blueLight;
    [SerializeField] private Image redBoost;
    [SerializeField] private Image blueBoost;
    [SerializeField] private Image obstacle;

    [Space] [SerializeField] private NoteAppearanceSO noteAppearance;

    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventsContainer events;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private PlatformDescriptor platform;

    // Start is called before the first frame update
    private void Start() => LoadInitialMap.PlatformLoadedEvent += LoadedPlatform;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;

    public void UpdateCustomColorsFromPacket(MapColorUpdatePacket packet)
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = redNote.color = packet.NoteLeft;
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.color = packet.NoteRight;
        noteAppearance.UpdateColor(packet.NoteLeft, packet.NoteRight);

        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.color =
            eventAppearance.RedColor = platform.Colors.RedColor = packet.LightLeft;
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.color = packet.LightRight;

        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.color =
            obstacleAppearance.DefaultObstacleColor = packet.Obstacle;


        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.color =
            eventAppearance.RedBoostColor = platform.Colors.RedBoostColor = packet.BoostLeft;
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = packet.BoostRight;

        // Little dangerous but should be OK
        BeatmapObjectContainerCollection.RefreshAllPools(true);
    }

    public MapColorUpdatePacket CreatePacketFromColors()
    {
        return new MapColorUpdatePacket()
        {
            NoteLeft = BeatSaberSongContainer.Instance.DifficultyData.ColorLeft ?? platform.DefaultColors.RedNoteColor,
            NoteRight = BeatSaberSongContainer.Instance.DifficultyData.ColorRight ?? platform.DefaultColors.BlueNoteColor,
            LightLeft = BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft ?? platform.DefaultColors.RedColor,
            LightRight = BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight ?? platform.DefaultColors.BlueColor,
            Obstacle = BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor ?? platform.DefaultColors.ObstacleColor,
            BoostLeft = BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft ?? platform.DefaultColors.RedBoostColor,
            BoostRight = BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight ?? platform.DefaultColors.BlueBoostColor
        };
    }

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
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.color =
            eventAppearance.RedColor = platform.Colors.RedColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
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
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = null;
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = null;
        blueNote.color = platform.DefaultColors.BlueNoteColor.WithAlpha(1);
        redNote.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
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

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = null;
        obstacleAppearance.DefaultObstacleColor = obstacle.color = platform.DefaultColors.ObstacleColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }
}
