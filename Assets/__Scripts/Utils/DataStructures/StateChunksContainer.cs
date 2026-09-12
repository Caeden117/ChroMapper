using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class StateChunksContainer<TState, TData> where TState : StateData<TData> where TData : BaseObject
{
    public readonly SortedBucketArray<TState> Collection = new(value => value?.StartTime ?? 0f, 10, 100);
    // GLS group moves can cross a float-derived bucket boundary, so removal must resolve the exact inserted identity directly.
    private readonly Dictionary<TData, TState> statesByBase = new();
    public TState CurrentState;

    private List<TState> currBucket;
    private int currBucketIdx;
    private int currLocalIdx;

    public void Resize(float max) => Collection.Resize((int)max);

    // Maintain the identity index alongside the ordered buckets so movement never scans or guesses a removal bucket.
    public void AddState(TState state)
    {
        Collection.Add(state);
        statesByBase[state.Base] = state;
    }

    // Cursor rounding belongs at load time; arbitrary paused lookups must preserve their exact time.
    public bool IsCurrentOrFindState(float time, bool playing) =>
        playing ? UseCurrentOrNextState(time) : UseCurrentOrFindState(time);

    private bool UseCurrentOrNextState(float time)
    {
        if (time < CurrentState.EndTime) return true;
        SetNextState(time);
        return false;
    }

    private void SetNextState(float time)
    {
        while (currBucketIdx < Collection.Buckets.Count)
        {
            currBucket = Collection.Buckets[currBucketIdx];
            while (currLocalIdx < currBucket.Count)
            {
                CurrentState = currBucket[currLocalIdx];
                if (CurrentState.IsWithinRange(time)) return;
                currLocalIdx++;
            }

            currLocalIdx = 0;
            currBucketIdx++;
        }
    }

    private bool UseCurrentOrFindState(float time)
    {
        if (CurrentState.IsWithinRange(time)) return true;
        SetStateAt(time);
        return false;
    }

    public void SetStateAt(float time)
    {
        var (bucketIdx, localIdx, state) = GetStateAt(time);
        currBucket = Collection.Buckets[bucketIdx];
        currBucketIdx = bucketIdx;
        currLocalIdx = localIdx;
        CurrentState = state;
    }

    public (int chunkIdx, int localIdx, TState state) GetStateAt(float time)
    {
        var bucket = Collection.GetBucketFrom(time);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = Collection.BinarySearchRight(bucket, time);

        if (idx == -1)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                idx = Collection.BinarySearchRight(bucket, time);
                if (idx != -1) break;
            }
        }

        return (bucketIdx, idx, bucket[idx]);
    }

    public TState GetPreviousStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = Collection.BinarySearchRight(bucket, state.StartTime) - 1;

        if (idx < 0)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = bucket.Count - 1;
        }

        return bucket[idx];
    }

    public TState GetOverlappingStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = Collection.BinarySearchRight(bucket, state.StartTime);

        if (idx < 0)
        {
            while (bucketIdx > 0)
            {
                bucket = Collection.Buckets[--bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = bucket.Count - 1;
        }

        return bucket[idx];
    }

    public TState GetNextStateFrom(TState state)
    {
        var bucket = Collection.GetBucketFrom(state.StartTime);
        var bucketIdx = Collection.Buckets.IndexOf(bucket);
        var idx = Collection.BinarySearchRight(bucket, state.StartTime) + 1;

        if (idx == -1 || idx == bucket.Count)
        {
            while (++bucketIdx < Collection.Buckets.Count)
            {
                bucket = Collection.Buckets[bucketIdx];
                if (bucket.Count != 0) break;
            }

            idx = 0;
        }

        return bucket[idx];
    }

    /// <summary>
    /// Gets a state from the container by reference.
    /// The reference must be the exact live instance originally inserted into the state bucket.
    /// </summary>
    public TState GetStateFrom(TData reference, TData _)
    {
        return statesByBase.TryGetValue(reference, out var state)
            ? state
            : null;
    }

    // Remove both representations together so a later group replacement cannot resolve a stale state identity.
    public bool RemoveState(TState state)
    {
        statesByBase.Remove(state.Base);
        return Collection.Remove(state);
    }
}
