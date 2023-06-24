using System;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class ObstacleGridContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject obstaclePrefab;
    [FormerlySerializedAs("obstacleAppearanceSO")][SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.Obstacle;

    public BaseObstacle[] SpawnSortedObjects;
    private int SpawnIndex;

    public BaseObstacle[] DespawnSortedObjects;
    private int DespawnIndex;

    public void SortSpawners()
    {
        SpawnSortedObjects = UnsortedObjects
            .Select(o => o as BaseObstacle)
            .OrderBy(o => o.SpawnJsonTime)
            .ToArray();
        DespawnSortedObjects = UnsortedObjects
            .Select(o => o as BaseObstacle)
            .OrderBy(o => o.DespawnJsonTime)
            .ToArray();
        var time = AudioTimeSyncController.CurrentJsonTime;
        GetIndexes(
            time,
            (i) => SpawnSortedObjects[i].SpawnJsonTime,
            SpawnSortedObjects.Length,
            out SpawnIndex,
            out var _
        );
        GetIndexes(
            time,
            (i) => DespawnSortedObjects[i].DespawnJsonTime,
            DespawnSortedObjects.Length,
            out DespawnIndex,
            out var _
        );
    }

    internal override void SubscribeToCallbacks()
    {
        Shader.SetGlobalFloat("_OutsideAlpha", 0.25f);
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        AudioTimeSyncController.TimeChanged += OnTimeChanged;
        UIMode.UIModeSwitched += OnUIModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        AudioTimeSyncController.TimeChanged -= OnTimeChanged;
        UIMode.UIModeSwitched -= OnUIModeSwitch;
    }

    private void OnPlayToggle(bool playing) => Shader.SetGlobalFloat("_OutsideAlpha", playing ? 0 : 0.25f);

    private void OnUIModeSwitch(UIModeType newMode)
    {
        // If preview mode disabled (enabled is handled by OnTimeChanged)
        if (newMode == UIModeType.Normal)
        {
            RefreshPool(true);
        }
    }

    public void UpdateColor(Color obstacle) => obstacleAppearanceSo.DefaultObstacleColor = obstacle;

    private bool updateFrame = false;
    internal override void LateUpdate()
    {
        if (!UIMode.AnimationMode)
            base.LateUpdate();
    }

    private void OnTimeChanged()
    {
        if (!UIMode.AnimationMode) return;
        // TODO: This should be somewhere else, want it to be called after all objects are added
        if (SpawnSortedObjects is null) {
            // Whyyyy
            if (UnsortedObjects.Count == 0) return;
            SortSpawners();
        }

        var time = AudioTimeSyncController.CurrentJsonTime;
        if (AudioTimeSyncController.IsPlaying)
        {
            while (SpawnIndex < SpawnSortedObjects.Length && time + 1f >= SpawnSortedObjects[SpawnIndex].SpawnJsonTime)
            {
                CreateContainerFromPool(SpawnSortedObjects[SpawnIndex]);
                ++SpawnIndex;
            }

            while (DespawnIndex < DespawnSortedObjects.Length && time - 1f >= DespawnSortedObjects[DespawnIndex].DespawnJsonTime)
            {
                var objectData = DespawnSortedObjects[DespawnIndex];
                if (LoadedContainers.ContainsKey(objectData))
                {
                    if (!LoadedContainers[objectData].Animator.AnimatedLife)
                        RecycleContainer(objectData);
                    else
                        LoadedContainers[objectData].Animator.ShouldRecycle = true;
                }
                ++DespawnIndex;
            }
        }
        else
        {
            foreach (var obj in UnsortedObjects)
            {
                RecycleContainer(obj);
            }
            GetIndexes(
                time,
                (i) => SpawnSortedObjects[i].SpawnJsonTime,
                SpawnSortedObjects.Length,
                out SpawnIndex,
                out var _
            );
            GetIndexes(
                time,
                (i) => DespawnSortedObjects[i].DespawnJsonTime,
                DespawnSortedObjects.Length,
                out DespawnIndex,
                out var _
            );
            var toSpawn = SpawnSortedObjects.Where(o => (o.SpawnJsonTime <= time && time < o.DespawnJsonTime));
            foreach (var obj in toSpawn)
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    protected override void OnObjectSpawned(BaseObject _, bool __ = false)
    {
        SortSpawners();
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);
    }

    protected override void OnObjectDelete(BaseObject _, bool __ = false)
    {
        SortSpawners();
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);
    }

    public override ObjectContainer CreateContainer()
    {
        var con = ObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.tracksManager = tracksManager;
        return con;
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var obstacle = con as ObstacleContainer;
        if (!obstacle.IsRotatedByNoodleExtensions && !obstacle.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(obj.SongBpmTime);
            track.AttachContainer(con);
        }

        obstacleAppearanceSo.SetObstacleAppearance(obstacle);
    }

    // Where is a good global place to dump this? It's much faster than List.BinarySearch
    private void GetIndexes(float time, Func<int, float> getter, int count, out int prev, out int next)
    {
        prev = 0;
        next = count;

        while (prev < next - 1)
        {
            int m = (prev + next) / 2;
            float itemTime = getter(m);

            if (itemTime < time)
            {
                prev = m;
            }
            else
            {
                next = m;
            }
        }
    }
}
