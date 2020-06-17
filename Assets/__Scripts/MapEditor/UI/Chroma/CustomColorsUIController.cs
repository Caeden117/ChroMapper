using System;
using System.Collections;
using System.Collections.Generic;
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
        if (redNote.color == BeatSaberSong.DEFAULT_LEFTNOTE) SetColorIfNotEqual(ref redNote, platform.RedNoteColor);
        if (blueNote.color == BeatSaberSong.DEFAULT_RIGHTNOTE) SetColorIfNotEqual(ref blueNote, platform.BlueNoteColor);
        if (redLight.color == BeatSaberSong.DEFAULT_LEFTCOLOR) SetColorIfNotEqual(ref redLight, platform.RedColor);
        if (blueLight.color == BeatSaberSong.DEFAULT_RIGHTCOLOR) SetColorIfNotEqual(ref blueLight, platform.BlueColor);
        if (obstacle.color == BeatSaberSong.DEFAULT_LEFTCOLOR) SetColorIfNotEqual(ref obstacle, platform.ObstacleColor);

        redNote.color = BeatSaberSongContainer.Instance.difficultyData.colorLeft ?? redNote.color;
        blueNote.color = BeatSaberSongContainer.Instance.difficultyData.colorRight ?? blueNote.color;
        redLight.color = BeatSaberSongContainer.Instance.difficultyData.envColorLeft ?? redLight.color;
        blueLight.color = BeatSaberSongContainer.Instance.difficultyData.envColorRight ?? blueLight.color;
        obstacle.color = BeatSaberSongContainer.Instance.difficultyData.obstacleColor ?? obstacle.color;

        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        platform.RedColor = eventAppearance.RedColor = redLight.color;
        platform.BlueColor = eventAppearance.BlueColor = blueLight.color;
        obstacleAppearance.defaultObstacleColor = obstacle.color;
    }

    private void SetColorIfNotEqual(ref Image a, Color b)
    {
        if (a.color != b) a.color = new Color(b.r, b.g, b.b, 1);
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
    }

    public void UpdateRedNote()
    {
        redNote.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = picker.CurrentColor;
        noteAppearance.UpdateColor(picker.CurrentColor, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void UpdateBlueNote()
    {
        blueNote.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = picker.CurrentColor;
        noteAppearance.UpdateColor(redNote.color, picker.CurrentColor);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void UpdateRedLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = picker.CurrentColor;
        eventAppearance.RedColor = redLight.color = platform.RedColor = picker.CurrentColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void UpdateBlueLight()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = picker.CurrentColor;
        eventAppearance.BlueColor = blueLight.color = platform.BlueColor = picker.CurrentColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void UpdateObstacles()
    {
        obstacle.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = picker.CurrentColor;
        obstacleAppearance.defaultObstacleColor = picker.CurrentColor;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
    }

    public void ResetNotes()
    {
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = null;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = null;
        blueNote.color = platform.BlueNoteColor;
        redNote.color = platform.RedNoteColor;
        noteAppearance.UpdateColor(redNote.color, blueNote.color);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).RefreshPool(true);
    }

    public void ResetLights()
    {
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = null;
        eventAppearance.RedColor = redLight.color = platform.RedColor = oldPlatformColorR;
        
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = null;
        eventAppearance.BlueColor = blueLight.color = platform.BlueColor = oldPlatformColorB;

        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public void ResetObstacles()
    {
        obstacle.color = BeatSaberSong.DEFAULT_LEFTCOLOR;
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = null;
        obstacleAppearance.defaultObstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE).RefreshPool(true);
    }
}
