using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class ObstacleGridContainer : BeatmapObjectContainerCollection<BaseObstacle>
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    public override ObjectType ContainerType => ObjectType.Obstacle;

    // OnTimeChanged can run during scene Start before map data reaches this collection. Initialize the sorted
    // arrays now so RefreshWalls can safely run before the first map load.
    public BaseObstacle[] SpawnSortedObjects = Array.Empty<BaseObstacle>();
    private int spawnIndex;

    public BaseObstacle[] DespawnSortedObjects = Array.Empty<BaseObstacle>();
    private int despawnIndex;
    private readonly TransitionIntervalIndex<BaseObstacle> wallLifetimes = new();
    private readonly HashSet<BaseObstacle> seekWalls = new();
    private FrozenWallGroup[] frozenWallGroups = Array.Empty<FrozenWallGroup>();

    private sealed class FrozenWallGroup
    {
        public string[] TrackNames;
        public readonly List<BaseObstacle> Walls = new();
    }

    private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");

    internal override void SubscribeToCallbacks()
    {
        // Rebuild the paused wall pool before OnTimeChangedEarly pushes held track values. Spawned wall
        // animators must exist when those values arrive so the seek applies them immediately.
        BeatmapContext.Atsc.OnTimeFlushPending += PrepareStoppedSeek;
        BeatmapContext.Atsc.OnTimeChanged += OnTimeChanged;
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggled;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;

        Settings.NotifyBySettingName(nameof(Settings.ObstacleOpacity), ObstacleOpacityChanged);
        ObstacleOpacityChanged(Settings.Instance.ObstacleOpacity);
    }

    private void HandlePlayToggled(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        foreach (ObstacleContainer obj in LoadedContainers.Values) obj.SetIndicators(!this.isPlaying);
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnTimeFlushPending -= PrepareStoppedSeek;
        BeatmapContext.Atsc.OnTimeChanged -= OnTimeChanged;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;

        Settings.ClearSettingNotifications(nameof(Settings.ObstacleOpacity));
    }

    private void ObstacleOpacityChanged(object obj) => Shader.SetGlobalFloat(mainAlpha, (float)obj);

    public override void RefreshPool(bool force)
    {
        if (UIMode.AnimationMode)
        {
            SpawnSortedObjects = MapObjects
                .OrderBy(GetSpawnTime)
                .ToArray();
            DespawnSortedObjects = MapObjects
                .OrderBy(GetDespawnTime)
                .ToArray();
            wallLifetimes.Clear();
            var groups = new Dictionary<string, FrozenWallGroup>();
            foreach (var wall in SpawnSortedObjects)
            {
                wallLifetimes.AddOrReplace(wall, GetSpawnTime(wall), GetDespawnTime(wall));
                if (wall.CustomTrack == null)
                    continue;
                var trackNamesKey = wall.CustomTrack.ToString();
                if (!groups.TryGetValue(trackNamesKey, out var group))
                {
                    group = new FrozenWallGroup { TrackNames = TrackDataReader.ReadNames(wall.CustomTrack) };
                    groups.Add(trackNamesKey, group);
                }
                group.Walls.Add(wall);
            }
            frozenWallGroups = groups.Values.ToArray();
            RefreshWalls(force);
        }
        else
        {
            base.RefreshPool(force);
        }
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public void UpdateColor(Color color) => obstacleAppearance.NormalColor = color;

    private bool updateFrame = false;
    private bool isPlaying;

    internal override void LateUpdate()
    {
        if (!UIMode.AnimationMode) base.LateUpdate();
    }

    private void PrepareStoppedSeek()
    {
        if (!UIMode.AnimationMode)
            return;
        RefreshWalls();
        foreach (ObstacleContainer wall in LoadedContainers.Values)
            wall.Animator.FlushPendingAnimations();
    }

    private void OnTimeChanged()
    {
        if (!UIMode.AnimationMode)
            return;
        if (!BeatmapContext.Atsc.IsPlaying)
        {
            foreach (ObstacleContainer wall in LoadedContainers.Values)
                wall.Animator.ApplyStoppedSeek();
            return;
        }

        var time = BeatmapContext.Atsc.CurrentSongBpmTime;
        while (spawnIndex < SpawnSortedObjects.Length
            && time + Track.JUMP_TIME >= GetSpawnTime(SpawnSortedObjects[spawnIndex]))
        {
            if (SpawnSortedObjects[spawnIndex].HasMatchingTrack(TrackFilterID))
                CreateContainerFromPool(SpawnSortedObjects[spawnIndex]);
            ++spawnIndex;
        }

        while (despawnIndex < DespawnSortedObjects.Length
            && time >= GetDespawnTime(DespawnSortedObjects[despawnIndex]))
        {
            var objectData = DespawnSortedObjects[despawnIndex];
            if (LoadedContainers.ContainsKey(objectData))
            {
                if (!LoadedContainers[objectData].Animator.AnimatedLife)
                    RecycleContainer(objectData);
                else
                    LoadedContainers[objectData].Animator.ShouldRecycle = true;
            }

            ++despawnIndex;
        }
    }

    private void RefreshWalls(bool force = false)
    {
        var time = BeatmapContext.Atsc.CurrentSongBpmTime;
        seekWalls.Clear();
        wallLifetimes.GetSourcesAt(time, seekWalls);
        // The shared transition index excludes its start and includes its end. Wall lifetimes use
        // the opposite boundaries, so add exact starts here and filter exact ends below.
        var firstSpawn = SpawnSortedObjects.AsSpan().LowerBoundBy(time, GetSpawnTime);
        for (var index = firstSpawn; index < SpawnSortedObjects.Length
            && GetSpawnTime(SpawnSortedObjects[index]) == time; index++)
            seekWalls.Add(SpawnSortedObjects[index]);

        seekWalls.RemoveWhere(wall => time >= GetDespawnTime(wall));
        var jsonTime = BeatmapContext.Atsc.CurrentJsonTime;
        // Heck uses the first authored track with a time property, even when it does not freeze the wall.
        foreach (var group in frozenWallGroups)
        {
            foreach (var name in group.TrackNames)
            {
                if (!TryGetTimeProperty(name, jsonTime, out var normalTime))
                    continue;
                if (normalTime >= 0f && normalTime <= 1f)
                {
                    foreach (var wall in group.Walls)
                    {
                        if (wall.SongBpmTime <= time)
                            seekWalls.Add(wall);
                    }
                }
                break;
            }
        }
        // Keep surviving animators and their track attachments. Recreating them on every slider
        // update reparses paths and repeatedly copies the map's large time-event invocation lists.
        for (var index = ObjectsWithContainers.Count - 1; index >= 0; index--)
        {
            var wall = (BaseObstacle)ObjectsWithContainers[index];
            if (force || !seekWalls.Contains(wall) || !wall.HasMatchingTrack(TrackFilterID))
                RecycleContainer(wall, index);
        }

        spawnIndex = Mathf.Max(0, SpawnSortedObjects.AsSpan().LowerBoundBy(time, GetSpawnTime) - 1);
        despawnIndex = Mathf.Max(0, DespawnSortedObjects.AsSpan().LowerBoundBy(time, GetDespawnTime) - 1);
        foreach (var obj in seekWalls)
        {
            if (!obj.HasMatchingTrack(TrackFilterID)) continue;
            var expired = time >= GetDespawnTime(obj);
            CreateContainerFromPool(obj);
            // The frozen wall outlived its raw window. Flag it so a later unfrozen animated time can recycle
            // it through ObjectAnimator's normal lifetime check.
            ((ObstacleContainer)LoadedContainers[obj]).Animator.ShouldRecycle = expired;
        }
    }

    private bool TryGetTimeProperty(string trackName, float jsonTime, out float normalized)
    {
        normalized = float.NaN;
        if (!tracksManager.TryGetAnimationTrack(trackName, out var animator) || animator == null)
            return false;
        foreach (var key in timePropertyKeys)
        {
            if (animator.AnimatedProperties.TryGetValue(key, out var property)
                && property is Beatmap.Animations.AnimateProperty<float> timeProperty
                && !timeProperty.IsEmpty())
            {
                normalized = timeProperty.GetLerpedValue(jsonTime);
                return true;
            }
        }
        return false;
    }

    private static readonly string[] timePropertyKeys = { "_time", "time" };

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    public override ObjectContainer CreateContainer()
    {
        var con = ObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);
        con.Animator.Context = BeatmapContext;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var obstacle = con as ObstacleContainer;
        obstacle.SwitchMaterial();
        if (!obstacle.IsRotatedByNoodleExtensions && !obstacle.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime, obstacle.ObstacleData.Rotation);
            track.AttachContainer(con);
        }

        obstacle.SetIndicators(!this.isPlaying);

        obstacleAppearance.SetObstacleAppearance(obstacle);
    }

    // Mixing a probed wall's beat with another wall's duration or jump offset can skip walls after a seek.
    private float GetSpawnTime(BaseObstacle obstacle) =>
        obstacle.SongBpmTime - Mathf.Max(obstacle.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats);

    private float GetDespawnTime(BaseObstacle obstacle) =>
        obstacle.SongBpmTime + obstacle.DurationSongBpmTime
            + Mathf.Max(obstacle.HalfJumpDuration, vNjsProvider.MaxHalfJumpDurationInBeats);
}
