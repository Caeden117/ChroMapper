using System;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

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
            RefreshWalls();
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
        if (UIMode.AnimationMode) RefreshWalls();
    }

    private void OnTimeChanged()
    {
        if (!UIMode.AnimationMode || !BeatmapContext.Atsc.IsPlaying) return;

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

    private void RefreshWalls()
    {
        var time = BeatmapContext.Atsc.CurrentSongBpmTime;
        foreach (var obj in LoadedContainers.Values.ToList())
        {
            RecycleContainer(obj.ObjectData);
        }

        spawnIndex = Mathf.Max(0, SpawnSortedObjects.AsSpan().LowerBoundBy(time, GetSpawnTime) - 1);
        despawnIndex = Mathf.Max(0, DespawnSortedObjects.AsSpan().LowerBoundBy(time, GetDespawnTime) - 1);
        // An AnimateTrack time property can keep a wall inside its animated lifetime after its raw spawn
        // window ends. During a stopped seek, retain walls whose evaluated normalized time remains in [0,1].
        // Ordinary expired walls still use the raw window.
        var jsonTime = BeatmapContext.Atsc.CurrentJsonTime;
        var toSpawn = SpawnSortedObjects.Where(o =>
            GetSpawnTime(o) <= time
                && (time < GetDespawnTime(o) || HasFrozenLifetimeAtSeek(o, time, jsonTime)));
        foreach (var obj in toSpawn)
        {
            if (!obj.HasMatchingTrack(TrackFilterID)) continue;
            var expired = time >= GetDespawnTime(obj);
            CreateContainerFromPool(obj);
            // The frozen wall outlived its raw window. Flag it so a later unfrozen animated time can recycle
            // it through ObjectAnimator's normal lifetime check.
            if (expired
                && LoadedContainers.TryGetValue(obj, out var spawned)
                && spawned is ObstacleContainer wall
                && wall.Animator != null)
            {
                wall.Animator.ShouldRecycle = true;
            }
        }
    }

    // Heck uses the first authored track with a time property, even if its value does not freeze the wall.
    // Only tracks without a time property fall through to the next track.
    private bool HasFrozenLifetimeAtSeek(BaseObstacle obj, float songBpmTime, float jsonTime)
    {
        if (obj.SongBpmTime > songBpmTime) return false;
        switch (obj.CustomTrack)
        {
            case SimpleJSON.JSONString name:
                return TryGetTimeProperty(name.Value, jsonTime, out var single)
                    && single >= 0f && single <= 1f;
            case SimpleJSON.JSONArray tracks:
                foreach (var node in tracks.Children)
                {
                    if (node is SimpleJSON.JSONString trackName
                        && TryGetTimeProperty(trackName.Value, jsonTime, out var normalTime))
                    {
                        return normalTime >= 0f && normalTime <= 1f;
                    }
                }
                return false;
            default:
                return false;
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
