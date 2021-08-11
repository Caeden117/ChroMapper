﻿using CustomFloorPlugin;
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public class LoadInitialMap : MonoBehaviour {

    [SerializeField] AudioTimeSyncController atsc;
    [SerializeField] RotationCallbackController rotationController;
    [Space]
    [SerializeField] NotesContainer notesContainer;
    [SerializeField] ObstaclesContainer obstaclesContainer;
    [SerializeField] MapLoader loader;
    [Space]
    [SerializeField] GameObject[] PlatformPrefabs;
    [SerializeField] GameObject[] DirectionalPlatformPrefabs;

    public static Action<PlatformDescriptor> PlatformLoadedEvent;
    public static Action LevelLoadedEvent;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap diff;
    private int totalObjectsToLoad = 0;
    private int totalObjectsLoaded = 0;

    void Awake()
    {
        SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());
    }

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.initialized); //Wait until Start has been called

        song = BeatSaberSongContainer.Instance.song; //Grab songe data
        diff = BeatSaberSongContainer.Instance.difficultyData;

        //Set up some local variables
        int environmentID = 0;
        bool customPlat = false;
        bool directional = false;

        environmentID = SongInfoEditUI.GetEnvironmentIDFromString(song.environmentName); //Grab platform by name (Official or Custom)
        if (song.customData != null && song.customData["_customEnvironment"] != null && song.customData["_customEnvironment"].Value != "")
        {
            if (CustomPlatformsLoader.Instance.GetAllEnvironmentIds().IndexOf(song.customData["_customEnvironment"] ?? "") >= 0) {
                customPlat = true;
            }
        }
        if (rotationController.IsActive && diff.parentBeatmapSet.beatmapCharacteristicName != "Lawless")
        {
            environmentID = SongInfoEditUI.GetDirectionalEnvironmentIDFromString(song.allDirectionsEnvironmentName);
            directional = true;
        }

        //Instantiate platform, grab descriptor
        GameObject platform = (customPlat ? CustomPlatformsLoader.Instance.LoadPlatform(song.customData["_customEnvironment"], (PlatformPrefabs[environmentID]) ?? PlatformPrefabs[0], null) : PlatformPrefabs[environmentID]) ?? PlatformPrefabs[0];
        if (directional && !customPlat) platform = DirectionalPlatformPrefabs[environmentID];
        GameObject instantiate = null;
        if (customPlat)
        {
            instantiate = platform;
        }
        else
        {
            Debug.Log("Instanciate nonCustomPlat");
            instantiate = Instantiate(platform, PlatformOffset, Quaternion.identity) as GameObject;
        }
        PlatformDescriptor descriptor = instantiate.GetComponent<PlatformDescriptor>();
        BeatmapEventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        descriptor.colors = descriptor.defaultColors.Clone();

        //Update Colors
        Color leftNote = BeatSaberSong.DEFAULT_LEFTNOTE; //Have default note as base
        if (descriptor.colors.RedNoteColor != BeatSaberSong.DEFAULT_LEFTCOLOR) leftNote = descriptor.colors.RedNoteColor; //Prioritize platforms
        if (diff.colorLeft != null) leftNote = diff.colorLeft ?? leftNote; //Then prioritize custom colors

        Color rightNote = BeatSaberSong.DEFAULT_RIGHTNOTE;
        if (descriptor.colors.BlueNoteColor != BeatSaberSong.DEFAULT_RIGHTCOLOR) rightNote = descriptor.colors.BlueNoteColor;
        if (diff.colorRight != null) rightNote = diff.colorRight ?? rightNote;

        notesContainer.UpdateColor(leftNote, rightNote);
        obstaclesContainer.UpdateColor(diff.obstacleColor ?? BeatSaberSong.DEFAULT_LEFTCOLOR);
        if (diff.colorLeft != null) descriptor.colors.RedNoteColor = diff.colorLeft ?? descriptor.colors.RedNoteColor;
        if (diff.colorRight != null) descriptor.colors.BlueNoteColor = diff.colorRight ?? descriptor.colors.BlueNoteColor;

        //We set light color to envColorLeft if it exists. If it does not exist, but colorLeft does, we use colorLeft.
        //If neither, we use default platform lights.
        if (diff.envColorLeft != null) descriptor.colors.RedColor = diff.envColorLeft ?? descriptor.colors.RedColor;
        else if (diff.colorLeft != null) descriptor.colors.RedColor = diff.colorLeft ?? descriptor.colors.RedColor;

        //Same thing for envColorRight
        if (diff.envColorRight != null) descriptor.colors.BlueColor = diff.envColorRight ?? descriptor.colors.BlueColor;
        else if (diff.colorRight != null) descriptor.colors.BlueColor = diff.colorRight ?? descriptor.colors.BlueColor;

        PlatformLoadedEvent.Invoke(descriptor); //Trigger event for classes that use the platform

        loader.UpdateMapData(BeatSaberSongContainer.Instance.map);
        yield return StartCoroutine(loader.HardRefresh());
        LevelLoadedEvent?.Invoke();
    }
}
