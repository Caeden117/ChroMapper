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
        LoadInitialMap.PlatformColorsRefreshedEvent += PlatformColorsChanged;
        SubscribeCustomColorButtons();
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
        LoadInitialMap.PlatformColorsRefreshedEvent -= PlatformColorsChanged;
        UnsubscribeCustomColorButtons();
    }

    public void UpdateCustomColorsFromPacket(MapColorUpdatePacket packet)
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft = redNote.image.color = packet.NoteLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight = blueNote.image.color = packet.NoteRight;
        noteAppearance.UpdateColor(packet.NoteLeft, packet.NoteRight);

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = platform.Colors.RedColor = packet.LightLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.image.color = packet.LightRight;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = eventAppearance.WhiteColor =
            platform.Colors.WhiteColor = whiteLight.image.color = packet.LightWhite;

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = obstacle.image.color =
            obstacleAppearance.DefaultObstacleColor = packet.Obstacle;

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = redBoost.image.color =
            eventAppearance.RedBoostColor = platform.Colors.RedBoostColor = packet.BoostLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = packet.BoostRight;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor = platform.Colors.WhiteBoostColor = packet.BoostWhite;

        // Little dangerous but should be OK
        BeatmapObjectContainerCollection.RefreshAllPools(true);
    }

    public MapColorUpdatePacket CreatePacketFromColors()
    {
        return new MapColorUpdatePacket()
        {
            NoteLeft = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft ?? platform.DefaultColors.RedNoteColor,
            NoteRight = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight ?? platform.DefaultColors.BlueNoteColor,
            LightLeft = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft ?? platform.DefaultColors.RedColor,
            LightRight = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight ?? platform.DefaultColors.BlueColor,
            LightWhite = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite ?? platform.DefaultColors.WhiteColor,
            Obstacle = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle ?? platform.DefaultColors.ObstacleColor,
            BoostLeft = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft ?? platform.DefaultColors.RedBoostColor,
            BoostRight = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight ?? platform.DefaultColors.BlueBoostColor,
            BoostWhite = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite ?? platform.DefaultColors.WhiteBoostColor
        };
    }

    private void PlatformColorsChanged(PlatformColors colors)
    {
        redLight.image.color = platform.Colors.RedColor;
        blueLight.image.color = platform.Colors.BlueColor;
        whiteLight.image.color = platform.Colors.WhiteColor;
        redBoost.image.color = platform.Colors.RedBoostColor;
        blueBoost.image.color = platform.Colors.BlueBoostColor;
        whiteBoost.image.color = platform.Colors.WhiteBoostColor;
        obstacle.image.color = obstacleAppearance.DefaultObstacleColor;
        
        CustomColorsUpdatedEvent?.Invoke();
    }

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;

        SetColorIfNotEqual(ref redNote, platform.Colors.RedNoteColor, DefaultColors.LeftNote,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft);
        SetColorIfNotEqual(ref blueNote, platform.Colors.BlueNoteColor, DefaultColors.RightNote,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight);
        SetColorIfNotEqual(ref redLight, platform.Colors.RedColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft);
        SetColorIfNotEqual(ref blueLight, platform.Colors.BlueColor, DefaultColors.Right,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight);
        SetColorIfNotEqual(ref whiteLight, platform.Colors.WhiteColor, DefaultColors.White,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite);
        SetColorIfNotEqual(ref redBoost, platform.Colors.RedBoostColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft);
        SetColorIfNotEqual(ref blueBoost, platform.Colors.BlueBoostColor, DefaultColors.Right,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight);
        SetColorIfNotEqual(ref whiteBoost, platform.Colors.WhiteBoostColor, DefaultColors.White,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite);
        SetColorIfNotEqual(ref obstacle, platform.Colors.ObstacleColor, DefaultColors.Left,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle);

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
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft = redNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight = blueNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = platform.Colors.RedColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = eventAppearance.BlueColor =
            platform.Colors.BlueColor = blueLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = eventAppearance.WhiteColor =
            platform.Colors.WhiteColor = whiteLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = platform.Colors.BlueBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor = platform.Colors.WhiteBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = obstacle.image.color =
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
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft = null;
        redNote.image.color = platform.DefaultColors.RedNoteColor.WithAlpha(1);
        RefreshNotes();
    }

    private void ResetBlueNote()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight = null;
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
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = null;
        redLight.image.color = eventAppearance.RedColor = platform.Colors.RedColor = platform.DefaultColors.RedColor;
        RefreshLights();
    }

    private void ResetBlueLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = null;
        blueLight.image.color = eventAppearance.BlueColor = platform.Colors.BlueColor = platform.DefaultColors.BlueColor;
        RefreshLights();
    }

    private void ResetWhiteLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = null;
        whiteLight.image.color = eventAppearance.WhiteColor = platform.Colors.WhiteColor = platform.DefaultColors.WhiteColor;
        RefreshLights();
    }

    private void ResetRedBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = null;
        redBoost.image.color = eventAppearance.RedBoostColor =
            platform.Colors.RedBoostColor = platform.DefaultColors.RedBoostColor;
        RefreshLights();
    }

    private void ResetBlueBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = null;
        blueBoost.image.color = eventAppearance.BlueBoostColor =
            platform.Colors.BlueBoostColor = platform.DefaultColors.BlueBoostColor;
        RefreshLights();
    }

    private void ResetWhiteBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = null;
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
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = null;
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
