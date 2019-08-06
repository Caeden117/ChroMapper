using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour {
    //Gets triggered whenever a Beatmap Object passes the grid.
    public static Action<BeatmapObject> OnBeatmapObjectPassGrid;
    //Gets triggered whenever a Beatmap Object passes the point to spawn.
    public static Action<BeatmapObject> OnBeatmapObjectPassSpawn;
    //Gets triggered whenever a Beatmap Object passes the point to despawn.
    public static Action<BeatmapObject> OnBeatmapObjectPassDespawn;

    public static GridController Instance;

    [SerializeField] public float SpawnDistance;
    [SerializeField] public float DespawnDistance;

    void Start()
    {
        Instance = this;
    }

    void Update() { }
}
