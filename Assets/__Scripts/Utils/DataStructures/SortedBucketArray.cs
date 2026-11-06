using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SortedBucketArray<T> : ICollection<T>
{
    private int count;
    public int Count => count;
    public bool IsReadOnly => false;

    public readonly List<List<T>> Buckets = new();
    private readonly int size;
    private readonly Func<T, float> getKeyValue;

    public SortedBucketArray(Func<T, float> keyValueFn, int size, int max)
    {
        getKeyValue = keyValueFn;
        this.size = size;
        Resize(max);
    }

    public void Resize(int max)
    {
        Buckets.Clear();
        for (var i = 0; i < (max / size) + 1; i++) Buckets.Add(new List<T>());
    }

    private int GetBucketIndex(float value) =>
        Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp(value, int.MinValue, int.MaxValue) / size), 0, Buckets.Count - 1);

    public int GetBucketIndexFrom(float value) => GetBucketIndex(value);
    public int GetBucketIndexFrom(T item) => GetBucketIndex(getKeyValue(item));

    public List<T> GetBucketFrom(T item) => Buckets[GetBucketIndex(getKeyValue(item))];
    public List<T> GetBucketFrom(float value) => Buckets[GetBucketIndex(value)];

    public IEnumerator<T> GetEnumerator()
    {
        var bucketIdx = 0;
        while (bucketIdx < Buckets.Count)
        {
            var chunk = Buckets[bucketIdx];
            var idx = 0;
            while (idx < chunk.Count)
            {
                yield return chunk[idx];
                idx++;
            }

            bucketIdx++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> EnumerateAfter(T item)
    {
        var bucketIdx = GetBucketIndex(getKeyValue(item));
        var bucket = Buckets[bucketIdx];
        var idx = bucket.IndexOf(item) + 1;
        while (bucketIdx < Buckets.Count)
        {
            bucket = Buckets[bucketIdx];
            while (idx < bucket.Count)
            {
                yield return bucket[idx];
                idx++;
            }

            idx = 0;
            bucketIdx++;
        }
    }

    public IEnumerator<T> EnumerateFrom(float value)
    {
        var bucketIdx = GetBucketIndex(value);
        var bucket = Buckets[bucketIdx];
        var idx = bucket.FindIndex(x => Mathf.Approximately(getKeyValue(x), value));

        while (bucketIdx >= 0)
        {
            bucket = Buckets[bucketIdx];

            while (idx >= 0)
            {
                if (getKeyValue(bucket[idx]) < value) break;
                idx--;
            }

            if (idx != -1 && getKeyValue(bucket[idx]) < value)
            {
                idx++;
                break;
            }

            bucketIdx--;
            if (bucketIdx != -1) idx = Buckets[bucketIdx].Count - 1;
        }

        if (bucketIdx == -1) yield break;
        while (bucketIdx < Buckets.Count)
        {
            bucket = Buckets[bucketIdx];
            while (idx < bucket.Count)
            {
                yield return bucket[idx];
                idx++;
            }

            idx = 0;
            bucketIdx++;
        }
    }

    public void Add(T item)
    {
        var bucket = GetBucketFrom(item);
        if (bucket.Count > 0)
        {
            if (getKeyValue(bucket[^1]) <= getKeyValue(item))
                bucket.Add(item);
            else
                bucket.Insert(BinarySearchRight(bucket, getKeyValue(item)) + 1, item);
        }
        else
            bucket.Add(item);

        count++;
    }

    // Detach the suffix before changing its sort keys so contractions cannot move entries into an unvisited bucket.
    public List<T> ExtractAfter(float value)
    {
        var extracted = new List<T>();
        var bucketIdx = GetBucketIndex(value);
        var index = BinarySearchRight(Buckets[bucketIdx], value) + 1;
        for (; bucketIdx < Buckets.Count; bucketIdx++)
        {
            var bucket = Buckets[bucketIdx];
            for (var i = index; i < bucket.Count; i++)
                extracted.Add(bucket[i]);

            var removed = bucket.Count - index;
            bucket.RemoveRange(index, removed);
            count -= removed;
            index = 0;
        }

        return extracted;
    }

    public int BinarySearchLeft(List<T> bucket, float target)
    {
        var left = 0;
        var right = bucket.Count - 1;

        while (left <= right)
        {
            var mid = left + ((right - left) / 2);
            if (getKeyValue(bucket[mid]) < target)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return left;
    }

    public int BinarySearchRight(List<T> bucket, float target)
    {
        var left = 0;
        var right = bucket.Count - 1;

        while (left <= right)
        {
            var mid = left + ((right - left) / 2);
            if (getKeyValue(bucket[mid]) <= target)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return right;
    }

    public bool Remove(T item)
    {
        var bucket = GetBucketFrom(item);
        if (!bucket.Remove(item)) return false;
        count--;
        return true;
    }

    public void CopyTo(T[] array, int arrayIdx)
    {
        foreach (var item in this) array[arrayIdx++] = item;
    }

    public void Clear()
    {
        for (var i = 0; i < Buckets.Count; i++) Buckets[i].Clear();
        count = 0;
    }

    public bool Contains(T item)
    {
        var bucket = GetBucketFrom(item);
        return bucket.Contains(item);
    }

    public int IndexOf(T item)
    {
        var bucket = GetBucketFrom(item);
        var idx = bucket.IndexOf(item);
        for (var i = 0; i < Buckets.Count && Buckets[i] != bucket; i++) idx += Buckets[i].Count;
        return idx;
    }
}
