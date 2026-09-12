using System;
using Beatmap.Appearances;
using Beatmap.Enums;
using UnityEngine;

public class CustomColorsUIController : MonoBehaviour
{
    public event Action OnCustomColorsUpdated;

    [SerializeField] private ColorPicker picker;

    [Space] [SerializeField] private CustomColorButton redNote;

    [SerializeField] private CustomColorButton blueNote;
    [SerializeField] private CustomColorButton redLight;
    [SerializeField] private CustomColorButton blueLight;
    [SerializeField] private CustomColorButton whiteLight;
    [SerializeField] private CustomColorButton redBoost;
    [SerializeField] private CustomColorButton blueBoost;
    [SerializeField] private CustomColorButton whiteBoost;
    [SerializeField] private CustomColorButton obstacle;

    [Space] [SerializeField] private NoteAppearanceSO noteAppearance;

    [SerializeField] private ObstacleGridContainer obstacleGrid;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private EventGridContainer eventGrid;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private ArcAppearanceSO arcAppearance;
    [SerializeField] private ChainAppearanceSO chainAppearance;

    public BeatmapRuntimeContext Context;

    // Start is called before the first frame update
    private void Start()
    {
        Context.OnColorSchemeChanged += HandleColorSchemeChanged;
        SubscribeCustomColorButtons();
    }

    private void OnDestroy()
    {
        Context.OnColorSchemeChanged -= HandleColorSchemeChanged;
        UnsubscribeCustomColorButtons();
    }

    public void UpdateCustomColorsFromPacket(MapColorUpdatePacket packet)
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft = redNote.image.color = packet.NoteLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight = blueNote.image.color = packet.NoteRight;
        noteAppearance.UpdateColor(packet.NoteLeft, packet.NoteRight);

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = Context.ColorScheme.EnvironmentLeftColor = packet.LightLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = eventAppearance.BlueColor =
            Context.ColorScheme.EnvironmentRightColor = blueLight.image.color = packet.LightRight;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = eventAppearance.WhiteColor =
            Context.ColorScheme.EnvironmentWhiteColor = whiteLight.image.color = packet.LightWhite;

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = obstacle.image.color =
            obstacleAppearance.NormalColor = packet.Obstacle;

        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = redBoost.image.color =
            eventAppearance.RedBoostColor =
                Context.ColorScheme.EnvironmentLeftBoostColor = packet.BoostLeft;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor =
                Context.ColorScheme.EnvironmentRightBoostColor = packet.BoostRight;
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor =
                Context.ColorScheme.EnvironmentWhiteBoostColor = packet.BoostWhite;

        // Little dangerous but should be OK
        BeatmapObjectContainerCollection.RefreshAllPools(true);
        // Packet-driven updates need to repaint UI subscribers just like direct picker edits.
        OnCustomColorsUpdated?.Invoke();
    }

    public MapColorUpdatePacket CreatePacketFromColors()
    {
        return new MapColorUpdatePacket()
        {
            NoteLeft =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft
                ?? Context.ColorScheme.LeftNoteColor,
            NoteRight =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight
                ?? Context.ColorScheme.RightNoteColor,
            LightLeft =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft
                ?? Context.ColorScheme.EnvironmentLeftColor,
            LightRight =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight
                ?? Context.ColorScheme.EnvironmentRightColor,
            LightWhite =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite
                ?? Context.ColorScheme.EnvironmentWhiteColor,
            Obstacle =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle
                ?? Context.ColorScheme.ObstacleColor,
            BoostLeft =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft
                ?? Context.ColorScheme.EnvironmentLeftBoostColor,
            BoostRight =
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight
                ?? Context.ColorScheme.EnvironmentRightBoostColor,
            BoostWhite = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite
                ?? Context.ColorScheme.EnvironmentWhiteBoostColor
        };
    }

    public void RefreshColors()
    {
        if (Context != null && Context.ColorScheme != null)
            HandleColorSchemeChanged(Context.ColorScheme);
    }

    private void HandleColorSchemeChanged(ColorSchemeSO colorScheme)
    {
        SetColor(
            ref redNote,
            Context.ColorScheme.LeftNoteColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft);
        SetColor(
            ref blueNote,
            Context.ColorScheme.RightNoteColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight);
        SetColor(
            ref redLight,
            Context.ColorScheme.EnvironmentLeftColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft);
        SetColor(
            ref blueLight,
            Context.ColorScheme.EnvironmentRightColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight);
        SetColor(
            ref whiteLight,
            Context.ColorScheme.EnvironmentWhiteColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite);
        SetColor(
            ref redBoost,
            Context.ColorScheme.EnvironmentLeftBoostColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft);
        SetColor(
            ref blueBoost,
            Context.ColorScheme.EnvironmentRightBoostColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight);
        SetColor(
            ref whiteBoost,
            Context.ColorScheme.EnvironmentWhiteBoostColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite);
        SetColor(
            ref obstacle,
            Context.ColorScheme.ObstacleColor,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle);

        Context.ColorScheme.EnvironmentLeftColor = eventAppearance.RedColor = redLight.image.color;
        Context.ColorScheme.EnvironmentRightColor = eventAppearance.BlueColor = blueLight.image.color;
        Context.ColorScheme.EnvironmentWhiteColor = eventAppearance.WhiteColor = whiteLight.image.color;
        Context.ColorScheme.EnvironmentLeftBoostColor =
            eventAppearance.RedBoostColor = redBoost.image.color;
        Context.ColorScheme.EnvironmentRightBoostColor =
            eventAppearance.BlueBoostColor = blueBoost.image.color;
        Context.ColorScheme.EnvironmentWhiteBoostColor =
            eventAppearance.WhiteBoostColor = whiteBoost.image.color;
        obstacleAppearance.NormalColor = obstacle.image.color;
    }

    // A missing Chroma override must follow each newly active OEM palette instead of retaining the prior environment color.
    private void SetColor(
        ref CustomColorButton colorButton,
        Color platformDefault,
        Color? savedColor)
    {
        // The environment palette has already populated these swatches before Info.dat colors arrive, so always replace it unless this difficulty has a custom Chroma override.
        var uiElement = colorButton.image;
        uiElement.color = (savedColor ?? platformDefault).WithAlpha(1f);
    }

    public void UpdateRedNote()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorLeft =
            redNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateBlueNote()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight =
            blueNote.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshNotes();
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = redLight.image.color =
            eventAppearance.RedColor = Context.ColorScheme.EnvironmentLeftColor =
                picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = eventAppearance.BlueColor =
            Context.ColorScheme.EnvironmentRightColor =
                blueLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = eventAppearance.WhiteColor =
            Context.ColorScheme.EnvironmentWhiteColor =
                whiteLight.image.color = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateRedBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = redBoost.image.color =
            eventAppearance.RedBoostColor =
                Context.ColorScheme.EnvironmentLeftBoostColor = picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateBlueBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = blueBoost.image.color =
            eventAppearance.BlueBoostColor = Context.ColorScheme.EnvironmentRightBoostColor =
                picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateWhiteBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = whiteBoost.image.color =
            eventAppearance.WhiteBoostColor = Context.ColorScheme.EnvironmentWhiteBoostColor =
                picker.CurrentColor.WithAlpha(1);
        RefreshLights();
    }

    public void UpdateObstacles()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = obstacle.image.color =
            obstacleAppearance.NormalColor = picker.CurrentColor.WithAlpha(1);
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
        redNote.image.color = Context.ColorScheme.LeftNoteColor.WithAlpha(1);
        RefreshNotes();
    }

    private void ResetBlueNote()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorRight = null;
        blueNote.image.color = Context.ColorScheme.RightNoteColor.WithAlpha(1);
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

        OnCustomColorsUpdated?.Invoke();
    }

    private void ResetRedLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorLeft = null;
        redLight.image.color = eventAppearance.RedColor = Context.ColorScheme.EnvironmentLeftColor =
            Context.ColorScheme.EnvironmentLeftColor;
        RefreshLights();
    }

    private void ResetBlueLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorRight = null;
        blueLight.image.color = eventAppearance.BlueColor = Context.ColorScheme.EnvironmentRightColor =
            Context.ColorScheme.EnvironmentRightColor;
        RefreshLights();
    }

    private void ResetWhiteLight()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorWhite = null;
        whiteLight.image.color = eventAppearance.WhiteColor =
            Context.ColorScheme.EnvironmentWhiteColor = Context.ColorScheme.EnvironmentWhiteColor;
        RefreshLights();
    }

    private void ResetRedBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostLeft = null;
        redBoost.image.color = eventAppearance.RedBoostColor =
            Context.ColorScheme.EnvironmentLeftBoostColor =
                Context.ColorScheme.EnvironmentLeftBoostColor;
        RefreshLights();
    }

    private void ResetBlueBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostRight = null;
        blueBoost.image.color = eventAppearance.BlueBoostColor =
            Context.ColorScheme.EnvironmentRightBoostColor =
                Context.ColorScheme.EnvironmentRightBoostColor;
        RefreshLights();
    }

    private void ResetWhiteBoost()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomEnvColorBoostWhite = null;
        whiteBoost.image.color = eventAppearance.WhiteBoostColor =
            Context.ColorScheme.EnvironmentWhiteBoostColor =
                Context.ColorScheme.EnvironmentWhiteBoostColor;
        RefreshLights();
    }

    private void RefreshLights()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        // Palette-based GLS colors need the same forced refresh as basic light events.
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSEvent).RefreshPool(true);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSColor).RefreshPool(true);
        OnCustomColorsUpdated?.Invoke();
    }

    public void ResetObstacles()
    {
        BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomColorObstacle = null;
        obstacleAppearance.NormalColor = obstacle.image.color = Context.ColorScheme.ObstacleColor;
        RefreshObstacles();
    }

    private void RefreshObstacles()
    {
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle).RefreshPool(true);
        OnCustomColorsUpdated?.Invoke();
    }

    private void SubscribeCustomColorButtons()
    {
        redNote.OnRightClick += SelectRedNote;
        redNote.OnMiddleClick += ResetRedNote;

        blueNote.OnRightClick += SelectBlueNote;
        blueNote.OnMiddleClick += ResetBlueNote;

        redLight.OnRightClick += SelectRedLight;
        redLight.OnMiddleClick += ResetRedLight;

        blueLight.OnRightClick += SelectBlueLight;
        blueLight.OnMiddleClick += ResetBlueLight;

        whiteLight.OnRightClick += SelectWhiteLight;
        whiteLight.OnMiddleClick += ResetWhiteLight;

        redBoost.OnRightClick += SelectRedBoost;
        redBoost.OnMiddleClick += ResetRedBoost;

        blueBoost.OnRightClick += SelectBlueBoost;
        blueBoost.OnMiddleClick += ResetBlueBoost;

        whiteBoost.OnRightClick += SelectWhiteBoost;
        whiteBoost.OnMiddleClick += ResetWhiteBoost;

        obstacle.OnRightClick += SelectObstacles;
        obstacle.OnMiddleClick += ResetObstacles;
    }

    private void UnsubscribeCustomColorButtons()
    {
        redNote.OnRightClick -= SelectRedNote;
        redNote.OnMiddleClick -= ResetRedNote;

        blueNote.OnRightClick -= SelectBlueNote;
        blueNote.OnMiddleClick -= ResetBlueNote;

        redLight.OnRightClick -= SelectRedLight;
        redLight.OnMiddleClick -= ResetRedLight;

        blueLight.OnRightClick -= SelectBlueLight;
        blueLight.OnMiddleClick -= ResetBlueLight;

        whiteLight.OnRightClick -= SelectWhiteLight;
        whiteLight.OnMiddleClick -= ResetWhiteLight;

        redBoost.OnRightClick -= SelectRedBoost;
        redBoost.OnMiddleClick -= ResetRedBoost;

        blueBoost.OnRightClick -= SelectBlueBoost;
        blueBoost.OnMiddleClick -= ResetBlueBoost;

        whiteBoost.OnRightClick -= SelectWhiteBoost;
        whiteBoost.OnMiddleClick -= ResetWhiteBoost;

        obstacle.OnRightClick -= SelectObstacles;
        obstacle.OnMiddleClick -= ResetObstacles;
    }
}
