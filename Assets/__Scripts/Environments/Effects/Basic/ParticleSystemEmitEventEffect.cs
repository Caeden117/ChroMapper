using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class ParticleSystemEmitEventEffect : BasicEventEffect<BasicEventStateData>
{
    [SerializeField] public ParticleSystemEventController Prefab;
    [SerializeField] public Transform ParticleSystemParentTransform;
    [SerializeField] public int MaxSpawnedParticleSystems = 4;

    private readonly BasicEventStateChunksContainer<BasicEventStateData> container = new();
    private readonly List<ParticleSystemEventController> activeControllers = new();
    private readonly Stack<ParticleSystemEventController> pooledControllers = new();
    private float previousBeat = float.NaN;
    private float previousSeconds = float.NaN;
    private bool rebuildRequested;
    private bool missingPrefabWarningShown;
    private bool invalidParentWarningShown;
    private bool invalidMaximumWarningShown;

    public override void Initialize()
    {
        InitializeStates(container);
        previousBeat = float.NaN;
        previousSeconds = float.NaN;
    }

    public override void Refresh()
    {
        container.SetStateAt(Atsc.CurrentSongBpmTime);
        Rebuild(Atsc.CurrentSongBpmTime);
        previousBeat = Atsc.CurrentSongBpmTime;
        previousSeconds = Atsc.CurrentSeconds;
    }

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        var stateChanged = !container.IsCurrentOrFindState(currentTime, isPlaying);
        var currentSeconds = Atsc.GetSecondsFromBeat(currentTime);
        var deltaTime = float.IsNaN(previousSeconds) ? 0f : currentSeconds - previousSeconds;
        var timeChanged = float.IsNaN(previousBeat) || !Mathf.Approximately(previousBeat, currentTime);
        var requiresRebuild = rebuildRequested
                              || (timeChanged
                                  && (float.IsNaN(previousBeat)
                                      || !isPlaying
                                      || deltaTime < 0f
                                      || deltaTime > 0.1f));

        if (requiresRebuild)
        {
            Rebuild(currentTime);
            rebuildRequested = false;
        }
        else if (stateChanged)
        {
            UpdateActiveControllers(currentSeconds, deltaTime);
            Spawn(container.CurrentState, currentSeconds);
        }
        else
            UpdateActiveControllers(currentSeconds, deltaTime);

        previousBeat = currentTime;
        previousSeconds = currentSeconds;
    }

    protected override BasicEventStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        HandleInsertState(container, state);
        rebuildRequested = true;
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var removedState = HandleRemoveState(container, reference, original);
        if (container.CurrentState == removedState) container.SetStateAt(reference.SongBpmTime);
        Rebuild(Atsc.CurrentSongBpmTime);
    }

    private void Rebuild(float currentBeat)
    {
        ReleaseAll();
        if (!CanSpawn()) return;

        var currentSeconds = Atsc.GetSecondsFromBeat(currentBeat);
        var matchingStates = new List<BasicEventStateData>();
        foreach (var bucket in container.Collection.Buckets)
        foreach (var state in bucket)
        {
            if (!IsEventState(state)) continue;
            var startTime = Atsc.GetSecondsFromBeat(state.StartTime);
            if (startTime <= currentSeconds && currentSeconds < startTime + Prefab.FullDuration)
                matchingStates.Add(state);
        }

        var first = Math.Max(0, matchingStates.Count - MaxSpawnedParticleSystems);
        for (var i = first; i < matchingStates.Count; i++) Spawn(matchingStates[i], currentSeconds);
    }

    private void Spawn(BasicEventStateData state, float currentSeconds)
    {
        if (!IsEventState(state) || !CanSpawn() || activeControllers.Count >= MaxSpawnedParticleSystems) return;

        var startTime = Atsc.GetSecondsFromBeat(state.StartTime);
        if (currentSeconds >= startTime + Prefab.FullDuration) return;

        var controller = pooledControllers.Count > 0
            ? pooledControllers.Pop()
            : Instantiate(Prefab, ParticleSystemParentTransform, false);
        controller.transform.SetParent(ParticleSystemParentTransform, false);
        controller.gameObject.SetActive(true);
        controller.Initialize(startTime);
        controller.ManualUpdate(currentSeconds, currentSeconds - startTime);
        activeControllers.Add(controller);
    }

    private void UpdateActiveControllers(float currentSeconds, float deltaTime)
    {
        for (var i = activeControllers.Count - 1; i >= 0; i--)
        {
            var controller = activeControllers[i];
            if (currentSeconds < controller.StartTime || currentSeconds >= controller.EndTime)
            {
                Release(controller);
                activeControllers.RemoveAt(i);
                continue;
            }

            controller.ManualUpdate(currentSeconds, deltaTime);
        }
    }

    private bool CanSpawn()
    {
        if (Prefab != null && ParticleSystemParentTransform != null && MaxSpawnedParticleSystems > 0) return true;
        if (Prefab == null && !missingPrefabWarningShown)
        {
            Debug.LogWarning($"[{nameof(ParticleSystemEmitEventEffect)}] No particle-system prefab is configured.");
            missingPrefabWarningShown = true;
        }
        if (ParticleSystemParentTransform == null && !invalidParentWarningShown)
        {
            Debug.LogWarning($"[{nameof(ParticleSystemEmitEventEffect)}] No particle-system parent is configured.");
            invalidParentWarningShown = true;
        }
        if (MaxSpawnedParticleSystems <= 0 && !invalidMaximumWarningShown)
        {
            Debug.LogWarning($"[{nameof(ParticleSystemEmitEventEffect)}] The maximum spawned-system count must be positive.");
            invalidMaximumWarningShown = true;
        }

        return false;
    }

    private static bool IsEventState(BasicEventStateData state) =>
        state.StartTime != short.MinValue && state.StartTime != float.MaxValue;

    private void Release(ParticleSystemEventController controller)
    {
        controller.Stop();
        controller.gameObject.SetActive(false);
        pooledControllers.Push(controller);
    }

    private void ReleaseAll()
    {
        for (var i = activeControllers.Count - 1; i >= 0; i--) Release(activeControllers[i]);
        activeControllers.Clear();
    }

    private void OnDestroy()
    {
        ReleaseAll();
        foreach (var controller in pooledControllers)
            if (controller != null)
                Destroy(controller.gameObject);
        pooledControllers.Clear();
    }
}
