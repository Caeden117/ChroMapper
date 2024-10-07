using System;
using Beatmap.Appearances;
using Beatmap.Enums;
using UnityEngine;

public class CustomColorsUIController : MonoBehaviour
{
    public event Action CustomColorsUpdatedEvent;

    [SerializeField] private ColorPicker picker;

    [Space][SerializeField] private CustomColorButton redNote;

    [SerializeField] private CustomColorButton blueNote;
    [SerializeField] private CustomColorButton redLight;
    [SerializeField] private CustomColorButton blueLight;
    [SerializeField] private CustomColorButton whiteLight;
    [SerializeField] private CustomColorButton redBoost;
    [SerializeField] private CustomColorButton blueBoost;
    [SerializeField] private CustomColorButton whiteBoost;
    [SerializeField] private CustomColorButton obstacle;

    [Space][SerializeField] private NoteAppearanceSO noteAppearance;

    [SerializeField] private ObstacleGridContainer obstacleGrid;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventGridContainer eventGrid;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private ArcAppearanceSO arcAppearance;
    [SerializeField] private ChainAppearanceSO chainAppearance;

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
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorWhite = eventAppearance.WhiteColor =
            platform.Colors.WhiteColor = whiteLight.image.color = packet.LightWhite;

        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.image.color =
            obstacleAppearance.DefaultObstacleColor = packet.Obstacle;

        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.image.color =
            eventAppearance.RedBoostColor = platform.Colors.RedBoostColor = packet.BoostLeft;
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = packet.BoostRight;
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor = platform.Colors.WhiteBoostColor = packet.BoostWhite;

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
            LightWhite = BeatSaberSongContainer.Instance.DifficultyData.EnvColorWhite ?? platform.DefaultColors.WhiteColor,
            Obstacle = BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor ?? platform.DefaultColors.ObstacleColor,
            BoostLeft = BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft ?? platform.DefaultColors.RedBoostColor,
            BoostRight = BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight ?? platform.DefaultColors.BlueBoostColor,
            BoostWhite = BeatSaberSongContainer.Instance.DifficultyData.BoostColorWhite ?? platform.DefaultColors.WhiteBoostColor
        };
    }

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;

        SetColorIfNotEqual(ref redNote, platform.Colors.RedNoteColor, DefaultColors.LeftNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorLeft);
        SetColorIfNotEqual(ref blueNote, platform.Colors.BlueNoteColor, DefaultColors.RightNote,
            BeatSaberSongContainer.Instance.DifficultyData.ColorRight);
        SetColorIfNotEqual(ref redLight, platform.Colors.RedColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft);
        SetColorIfNotEqual(ref blueLight, platform.Colors.BlueColor, DefaultColors.Right,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight);
        SetColorIfNotEqual(ref whiteLight, platform.Colors.WhiteColor, DefaultColors.White,
            BeatSaberSongContainer.Instance.DifficultyData.EnvColorWhite);
        SetColorIfNotEqual(ref redBoost, platform.Colors.RedBoostColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft);
        SetColorIfNotEqual(ref blueBoost, platform.Colors.BlueBoostColor, DefaultColors.Right,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight);
        SetColorIfNotEqual(ref whiteBoost, platform.Colors.WhiteBoostColor, DefaultColors.White,
            BeatSaberSongContainer.Instance.DifficultyData.BoostColorWhite);
        SetColorIfNotEqual(ref obstacle, platform.Colors.ObstacleColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor);

        platform.Colors.RedColor = eventAppearance.RedColor = redLight.image.color;
        platform.Colors.BlueColor = eventAppearance.BlueColor = blueLight.image.color;
        platform.Colors.WhiteColor = eventAppearance.WhiteColor = whiteLight.image.color;
        platform.Colors.RedBoostColor = eventAppearance.RedBoostColor = redBoost.image.color;
        platform.Colors.BlueBoostColor = eventAppearance.BlueBoostColor = blueBoost.image.color;
        platform.Colors.WhiteBoostColor = eventAppearance.WhiteBoostColor = whiteBoost.image.color;
        obstacleAppearance.DefaultObstacleColor = obstacle.image.color;
    }

    private void SetColorIfNotEqual(ref CustomColorButton colorButton, Color platformDefault, Color @default, Color? savedColor)
    {
        var uiElement = colorButton.image;
        if (uiElement.color == @default && uiElement.color != platformDefault)
            uiElement.color = platformDefault.WithAlpha(1);
        uiElement.color = savedColor ?? uiElement.color;
    }

    public void UpdateRedNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = redNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = blueNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = platform.Colors.RedColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorWhite = eventAppearance.WhiteColor =
            platform.Colors.WhiteColor = whiteLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorLeft = redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor = platform.Colors.WhiteBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = obstacle.image.color =
            obstacleAppearance.DefaultObstacleColor = picker.CurrentColor.WithAlpha(1);
        RefreshObstacles();
    }

    private void SelectRedNote() => picker.CurrentColor = redNote.image.color;
    private void SelectBlueNote() => picker.CurrentColor = blueNote.image.color;
    private void SelectRedLight() => picker.CurrentColor = redLight.image.color;
    private void SelectBlueLight() => picker.CurrentColor = blueLight.image.color;
    private void SelectWhiteLight() => picker.CurrentColor = whiteLight.image.color;
    private void SelectRedBoost() => picker.CurrentColor = redBoost.image.color;
    private void SelectBlueBoost() => picker.CurrentColor = blueBoost.image.color;
    private void SelectWhiteBoost() => picker.CurrentColor = whiteBoost.image.color;
    private void SelectObstacles() => picker.CurrentColor = obstacle.image.color;


    private void ResetRedNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorLeft = null;
        redNote.image.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        RefreshNotes();
    }

    private void ResetBlueNote()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ColorRight = null;
        blueNote.image.color = platform.DefaultColors.BlueNoteColor.WithAlpha(1);
        RefreshNotes();
    }

    private void RefreshNotes()
    {
        noteAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        arcAppearance.UpdateColor(redNote.image.color, blueNote.image.color);
        chainAppearance.UpdateColor(redNote.image.color, blueNote.image.color);

        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note).RefreshPool(true);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc).RefreshPool(true);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain).RefreshPool(true);

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

    private void ResetWhiteLight()
    {
        BeatSaberSongContainer.Instance.DifficultyData.EnvColorWhite = null;
        whiteLight.image.color = eventAppearance.WhiteColor = platform.Colors.WhiteColor = platform.DefaultColors.WhiteColor;
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

    private void ResetWhiteBoost()
    {
        BeatSaberSongContainer.Instance.DifficultyData.BoostColorWhite = null;
        whiteBoost.image.color = eventAppearance.WhiteBoostColor =
            platform.Colors.WhiteBoostColor = platform.DefaultColors.WhiteBoostColor;
        RefreshLights();
    }

    private void RefreshLights()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        CustomColorsUpdatedEvent?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.DifficultyData.ObstacleColor = null;
        obstacleAppearance.DefaultObstacleColor = obstacle.image.color = platform.DefaultColors.ObstacleColor;
        RefreshObstacles();
    }

    private void RefreshObstacles()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle).RefreshPool(true);
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

        whiteLight.onRightClick += SelectWhiteLight;
        whiteLight.onMiddleClick += ResetWhiteLight;

        redBoost.onRightClick += SelectRedBoost;
        redBoost.onMiddleClick += ResetRedBoost;

        blueBoost.onRightClick += SelectBlueBoost;
        blueBoost.onMiddleClick += ResetBlueBoost;

        whiteBoost.onRightClick += SelectWhiteBoost;
        whiteBoost.onMiddleClick += ResetWhiteBoost;

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

        whiteLight.onRightClick -= SelectWhiteLight;
        whiteLight.onMiddleClick -= ResetWhiteLight;

        redBoost.onRightClick -= SelectRedBoost;
        redBoost.onMiddleClick -= ResetRedBoost;

        blueBoost.onRightClick -= SelectBlueBoost;
        blueBoost.onMiddleClick -= ResetBlueBoost;

        whiteBoost.onRightClick -= SelectWhiteBoost;
        whiteBoost.onMiddleClick -= ResetWhiteBoost;

        obstacle.onRightClick -= SelectObstacles;
        obstacle.onMiddleClick -= ResetObstacles;
    }
}
