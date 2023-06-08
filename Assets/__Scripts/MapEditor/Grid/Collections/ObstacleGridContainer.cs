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
            .OrderBy(o => o.JsonTime - o.Hjd)
            .ToArray();
        DespawnSortedObjects = UnsortedObjects
            .Select(o => o as BaseObstacle)
            .OrderBy(o => o.JsonTime + o.Duration + o.Hjd)
            .ToArray();
    }

    internal override void SubscribeToCallbacks()
    {
        Shader.SetGlobalFloat("_OutsideAlpha", 0.25f);
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        AudioTimeSyncController.TimeChanged += OnTimeChanged;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        AudioTimeSyncController.TimeChanged -= OnTimeChanged;
    }

    private void OnPlayToggle(bool playing) => Shader.SetGlobalFloat("_OutsideAlpha", playing ? 0 : 0.25f);

    public void UpdateColor(Color obstacle) => obstacleAppearanceSo.DefaultObstacleColor = obstacle;

    private bool updateFrame = false;
    internal override void LateUpdate()
    {
        if (!updateFrame) return;
        updateFrame = false;

        var time = AudioTimeSyncController.CurrentJsonTime;
        if (AudioTimeSyncController.IsPlaying)
        {
            while (SpawnIndex < SpawnSortedObjects.Length && time >= SpawnSortedObjects[SpawnIndex].JsonTime - SpawnSortedObjects[SpawnIndex].Hjd)
            {
                CreateContainerFromPool(SpawnSortedObjects[SpawnIndex]);
                ++SpawnIndex;
            }

            while (DespawnIndex < DespawnSortedObjects.Length && time >= DespawnSortedObjects[DespawnIndex].JsonTime + DespawnSortedObjects[DespawnIndex].Duration + DespawnSortedObjects[DespawnIndex].Hjd)
            {
                RecycleContainer(DespawnSortedObjects[DespawnIndex]);
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
                (i) => SpawnSortedObjects[i].JsonTime - SpawnSortedObjects[i].Hjd,
                SpawnSortedObjects.Length,
                out var _,
                out SpawnIndex
            );
            GetIndexes(
                time,
                (i) => DespawnSortedObjects[i].JsonTime + DespawnSortedObjects[i].Duration + DespawnSortedObjects[i].Hjd,
                DespawnSortedObjects.Length,
                out var _,
                out DespawnIndex
            );
            Debug.Log($"{SpawnIndex} {DespawnIndex}");
            var toSpawn = SpawnSortedObjects.Where(o => o.JsonTime - o.Hjd <= time && time < o.JsonTime + o.Duration + o.Hjd);
            foreach (var obj in toSpawn)
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    private void OnTimeChanged()
    {
        // TODO: This should be somewhere else, want it to be called after all objects are added
        if (SpawnSortedObjects is null) {
            // Whyyyy
            if (UnsortedObjects.Count == 0) return;
            SortSpawners();
        }
        // spawning needs to happen in LateUpdate or else default cube jumpscare
        updateFrame = true;
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
            var track = tracksManager.GetTrackAtTime(obj.JsonTime);
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
