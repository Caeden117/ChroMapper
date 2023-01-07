using System;
using UnityEngine;
using UnityEngine.UI;

public class CustomColorsUIController : MonoBehaviour
{
    public event Action CustomColorsUpdatedEvent;

    [SerializeField] private ColorPicker picker;

    [Space][SerializeField] private CustomColorButton redNote;

    [SerializeField] private CustomColorButton blueNote;
    [SerializeField] private CustomColorButton redLight;
    [SerializeField] private CustomColorButton blueLight;
    [SerializeField] private CustomColorButton redBoost;
    [SerializeField] private CustomColorButton blueBoost;
    [SerializeField] private CustomColorButton obstacle;

    [Space][SerializeField] private NoteAppearanceSO noteAppearance;

    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventsContainer events;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private PlatformDescriptor platform;

    // Start is called before the first frame update
    private void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += LoadedPlatform;
        SubscribeCustomColorButtons();
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
        UnsubscribeCustomColorButtons();
    }

    public void UpdateCustomColorsFromPacket(MapColorUpdatePacket packet)
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = redNote.image.color = packet.NoteLeft;
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.image.color = packet.NoteRight;
        noteAppearance.UpdateColor(packet.NoteLeft, packet.NoteRight);

        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = platform.Colors.RedColor = packet.LightLeft;
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.image.color = packet.LightRight;

        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.image.color =
            obstacleAppearance.DefaultObstacleColor = packet.Obstacle;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.image.color =
            eventAppearance.RedBoostColor = platform.Colors.RedBoostColor = packet.BoostLeft;
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.image.color =
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

        SetColorIfNotEqual(ref redNote.image, platform.Colors.RedNoteColor, BeatSaberSong.DefaultLeftNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorLeft);
        SetColorIfNotEqual(ref blueNote.image, platform.Colors.BlueNoteColor, BeatSaberSong.DefaultRightNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorRight);
        SetColorIfNotEqual(ref redLight.image, platform.Colors.RedColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft);
        SetColorIfNotEqual(ref blueLight.image, platform.Colors.BlueColor, BeatSaberSong.DefaultRightColor,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight);
        SetColorIfNotEqual(ref redBoost.image, platform.Colors.RedBoostColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft);
        SetColorIfNotEqual(ref blueBoost.image, platform.Colors.BlueBoostColor, BeatSaberSong.DefaultRightColor,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight);
        SetColorIfNotEqual(ref obstacle.image, platform.Colors.ObstacleColor, BeatSaberSong.DefaultLeftColor,
            BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor);

        platform.Colors.RedColor = eventAppearance.RedColor = redLight.image.color;
        platform.Colors.BlueColor = eventAppearance.BlueColor = blueLight.image.color;
        platform.Colors.RedBoostColor = eventAppearance.RedBoostColor = redBoost.image.color;
        platform.Colors.BlueBoostColor = eventAppearance.BlueBoostColor = blueBoost.image.color;
        obstacleAppearance.DefaultObstacleColor = obstacle.image.color;
    }

    private void SetColorIfNotEqual(ref Image uiElement, Color platformDefault, Color @default, Color? savedColor)
    {
        if (uiElement.color == @default && uiElement.color != platformDefault)
            uiElement.color = platformDefault.WithAlpha(1);
        uiElement.color = savedColor ?? uiElement.color;
    }

    public void UpdateRedNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = redNote.image.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.image.color = picker.CurrentColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = platform.Colors.RedColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.image.color = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = picker.CurrentColor.WithAlpha(1);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.image.color =
            obstacleAppearance.DefaultObstacleColor = picker.CurrentColor.WithAlpha(1);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    private void SelectRedNote() => picker.CurrentColor = redNote.image.color;
    private void SelectBlueNote() => picker.CurrentColor = blueNote.image.color;
    private void SelectRedLight() => picker.CurrentColor = redLight.image.color;
    private void SelectBlueLight() => picker.CurrentColor = blueLight.image.color;
    private void SelectRedBoost() => picker.CurrentColor = redBoost.image.color;
    private void SelectBlueBoost() => picker.CurrentColor = blueBoost.image.color;
    private void SelectObstacles() => picker.CurrentColor = obstacle.image.color;


    private void ResetRedNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = null;
        redNote.image.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        RefreshNotes();
    }

    private void ResetBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = null;
        blueNote.image.color = platform.DefaultColors.BlueNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        RefreshNotes();
    }

    private void RefreshNotes()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    private void ResetRedLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = null;
        redLight.image.color = eventAppearance.RedColor = platform.Colors.RedColor = platform.DefaultColors.RedColor;
        RefreshLights();
    }

    private void ResetBlueLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = null;
        blueLight.image.color = eventAppearance.BlueColor = platform.Colors.BlueColor = platform.DefaultColors.BlueColor;
        RefreshLights();
    }

    private void ResetRedBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = null;
        redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = platform.DefaultColors.RedBoostColor;
        RefreshLights();
    }

    private void ResetBlueBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = null;
        blueBoost.image.color = eventAppearance.BlueBoostColor =
            platform.Colors.BlueBoostColor = platform.DefaultColors.BlueBoostColor;
        RefreshLights();
    }

    private void RefreshLights()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = null;
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = null;
        blueNote.image.color = platform.DefaultColors.BlueNoteColor.WithAlpha(1);
        redNote.image.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetLights()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = null;
        redLight.image.color = eventAppearance.RedColor = platform.Colors.RedColor = platform.DefaultColors.RedColor;

        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = null;
        blueLight.image.color = eventAppearance.BlueColor = platform.Colors.BlueColor = platform.DefaultColors.BlueColor;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = null;
        redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = platform.DefaultColors.RedBoostColor;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = null;
        blueBoost.image.color = eventAppearance.BlueBoostColor =
            platform.Colors.BlueBoostColor = platform.DefaultColors.BlueBoostColor;

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = null;
        obstacleAppearance.DefaultObstacleColor = obstacle.image.color = platform.DefaultColors.ObstacleColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    private void SubscribeCustomColorButtons()
    {
        redNote.onRightClick += SelectRedNote;
        redNote.onMiddleClick += ResetRedNote;

        blueNote.onRightClick += SelectBlueNote;
        blueNote.onMiddleClick += ResetBlueNote;

        redLight.onRightClick += SelectRedLight;
        redLight.onMiddleClick += ResetRedLight;

        blueLight.onRightClick += SelectBlueLight;
        blueLight.onMiddleClick += ResetBlueLight;

        redBoost.onRightClick += SelectRedBoost;
        redBoost.onMiddleClick += ResetRedBoost;

        blueBoost.onRightClick += SelectBlueBoost;
        blueBoost.onMiddleClick += ResetBlueBoost;

        obstacle.onRightClick += SelectObstacles;
        obstacle.onMiddleClick += ResetObstacles;
    }

    private void UnsubscribeCustomColorButtons()
    {
        redNote.onRightClick -= SelectRedNote;
        redNote.onMiddleClick -= ResetRedNote;

        blueNote.onRightClick -= SelectBlueNote;
        blueNote.onMiddleClick -= ResetBlueNote;

        redLight.onRightClick -= SelectRedLight;
        redLight.onMiddleClick -= ResetRedLight;

        blueLight.onRightClick -= SelectBlueLight;
        blueLight.onMiddleClick -= ResetBlueLight;

        redBoost.onRightClick -= SelectRedBoost;
        redBoost.onMiddleClick -= ResetRedBoost;

        blueBoost.onRightClick -= SelectBlueBoost;
        blueBoost.onMiddleClick -= ResetBlueBoost;

        obstacle.onRightClick -= SelectObstacles;
        obstacle.onMiddleClick -= ResetObstacles;
    }
}
