using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        redNote.color = BeatSaberSongContainer.Instance.difficultyData.colorLeft;
        blueNote.color = BeatSaberSongContainer.Instance.difficultyData.colorRight;
        redLight.color = BeatSaberSongContainer.Instance.difficultyData.envColorLeft;
        blueLight.color = BeatSaberSongContainer.Instance.difficultyData.envColorRight;
        obstacle.color = BeatSaberSongContainer.Instance.difficultyData.obstacleColor;
    }

    private void LoadedPlatform(PlatformDescriptor obj)
    {
        platform = obj;
        oldPlatformColorR = platform.RedColor;
        oldPlatformColorB = platform.BlueColor;
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= LoadedPlatform;
    }

    public void UpdateRedNote()
    {
        redNote.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = picker.CurrentColor;
        noteAppearance.UpdateColor(picker.CurrentColor, BeatSaberSongContainer.Instance.difficultyData.colorRight);
    }

    public void UpdateBlueNote()
    {
        blueNote.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = picker.CurrentColor;
        noteAppearance.UpdateColor(BeatSaberSongContainer.Instance.difficultyData.colorLeft, picker.CurrentColor);
    }

    public void UpdateRedLight()
    {
        redLight.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = picker.CurrentColor;
        platform.RedColor = picker.CurrentColor;
        foreach (BeatmapObjectContainer con in events.LoadedContainers)
            eventAppearance.SetEventAppearance(con as BeatmapEventContainer);
    }

    public void UpdateBlueLight()
    {
        blueLight.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = picker.CurrentColor;
        platform.BlueColor = picker.CurrentColor;
        foreach (BeatmapObjectContainer con in events.LoadedContainers)
            eventAppearance.SetEventAppearance(con as BeatmapEventContainer);
    }

    public void UpdateObstacles()
    {
        obstacle.color = picker.CurrentColor;
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = picker.CurrentColor;
        obstacleAppearance.defaultObstacleColor = picker.CurrentColor;
        foreach (BeatmapObjectContainer con in obstacles.LoadedContainers)
            obstacleAppearance.SetObstacleAppearance(con as BeatmapObstacleContainer);
    }

    public void ResetNotes()
    {
        redNote.color = BeatSaberSong.DEFAULT_LEFTNOTE;
        BeatSaberSongContainer.Instance.difficultyData.colorLeft = picker.CurrentColor;
        blueNote.color = BeatSaberSong.DEFAULT_RIGHTNOTE;
        BeatSaberSongContainer.Instance.difficultyData.colorRight = BeatSaberSong.DEFAULT_RIGHTNOTE;
        noteAppearance.UpdateColor(BeatSaberSong.DEFAULT_LEFTNOTE, BeatSaberSong.DEFAULT_RIGHTNOTE);
    }

    public void ResetLights()
    {

        redLight.color = BeatSaberSong.DEFAULT_LEFTCOLOR;
        BeatSaberSongContainer.Instance.difficultyData.envColorLeft = BeatSaberSong.DEFAULT_LEFTCOLOR;
        platform.RedColor = oldPlatformColorR;
        blueLight.color = BeatSaberSong.DEFAULT_RIGHTCOLOR;
        BeatSaberSongContainer.Instance.difficultyData.envColorRight = BeatSaberSong.DEFAULT_RIGHTCOLOR;
        platform.BlueColor = oldPlatformColorB;
        foreach (BeatmapObjectContainer con in events.LoadedContainers)
            eventAppearance.SetEventAppearance(con as BeatmapEventContainer);
    }

    public void ResetObstacles()
    {
        obstacle.color = BeatSaberSong.DEFAULT_LEFTCOLOR;
        BeatSaberSongContainer.Instance.difficultyData.obstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
        obstacleAppearance.defaultObstacleColor = BeatSaberSong.DEFAULT_LEFTCOLOR;
        foreach (BeatmapObjectContainer con in obstacles.LoadedContainers)
            obstacleAppearance.SetObstacleAppearance(con as BeatmapObstacleContainer);
    }
}
