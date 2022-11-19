using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightEventFilter: BeatmapObject
{
    public int FilterType; // f
    public int Partition; // p
    public int Section; // t
    public int Reverse; // r
    // introduced in 3.1
    public int Chunk; // c
    public int RandomType; // n
    public int RandomSeed; // s
    public int Limit; // l
    public int AffectAlso; // d

    public BeatmapLightEventFilter(JSONNode node)
    {
        FilterType = RetrieveRequiredNode(node, "f").AsInt;
        Partition = RetrieveRequiredNode(node, "p").AsInt;
        Section = RetrieveRequiredNode(node, "t").AsInt;
        Reverse = RetrieveRequiredNode(node, "r").AsInt;

        Chunk = node.HasKey("c") ? node["c"].AsInt : 0;
        RandomType = node.HasKey("n") ? node["n"].AsInt : 0;
        RandomSeed = node.HasKey("s") ? node["s"].AsInt : 0;
        Limit = node.HasKey("l") ? node["l"].AsInt : 0;
        AffectAlso = node.HasKey("d") ? node["d"].AsInt : 0;
    }

    public BeatmapLightEventFilter() 
    {
        FilterType = 1;
        Partition = 1;
    }
    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        var node = new JSONObject();
        node["f"] = FilterType;
        node["p"] = Partition;
        node["t"] = Section;
        node["r"] = Reverse;

        node["c"] = Chunk;
        node["n"] = RandomType;
        node["s"] = RandomSeed;
        node["l"] = Limit;
        node["d"] = AffectAlso;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();

    public static bool SanityCheck(BeatmapLightEventFilter e)
    {
        if (e.FilterType == 1)
        {
            if (e.Section < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Section must be positive.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
            if (e.Partition <= 0)
            {
                PersistentUI.Instance.ShowDialogBox("Partition must be positive.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
        }
        else if (e.FilterType == 2)
        {
            if (e.Section < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Step must be non-negative.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
            if (e.Partition < 0)
            {
                PersistentUI.Instance.ShowDialogBox("Start index must be non-negative.", null, PersistentUI.DialogBoxPresetType.Ok);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Giving a list of elements, return filtered list[chunk][element]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns>return a list of chunks, all the elements in each chunk will happen at the same time</returns>
    public IEnumerable<IEnumerable<T>> Filter<T>(IEnumerable<T> list)
    {
        return Filter(list, this);
    }

    /// <summary>
    /// filter pipeline will be chunk -> fraction/range -> shuffle -> limit
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static IEnumerable<IEnumerable<T>> Filter<T>(IEnumerable<T> list, BeatmapLightEventFilter filter)
    {
        if (filter.Chunk != 0)
        {
            float chunkSize = list.Count() / (float)filter.Chunk;
            var chunkList = list
                .Select((filter, idx) => (filter, idx))
                .GroupBy(t => Mathf.FloorToInt(t.idx / chunkSize));
            var filterChunk = SubFilter(chunkList, filter);
            var ret = filterChunk.Select(g => g.Select(t => t.filter));
            return ret;
        }
        else
        {
            var filterList = SubFilter(list, filter);
            // only one element in each group;
            var ret = filterList.Select(x => new[] { x });
            return ret;
        }
    }

    private static IEnumerable<T> SubFilter<T>(IEnumerable<T> list, BeatmapLightEventFilter filter)
    {
        if (filter.FilterType == 1)
        {
            list = Fraction(list, filter.Section, filter.Partition, filter.Reverse == 1);
        }
        else if (filter.FilterType == 2)
        {
            list = Range(list, filter.Partition, filter.Section, filter.Reverse == 1);
        }
        if (filter.RandomType == 2)
        {
            Random.InitState(filter.RandomSeed);
            list = Shuffle(list);
        }
        if (filter.Limit != 0)
        {
            int limit = Mathf.RoundToInt(list.Count() * filter.Limit / 100.0f);
            list = list.Where((x, i) => i < limit);
        }
        return list;
    }

    private static IEnumerable<T> Shuffle<T>(IEnumerable<T> list)
    {
        var a = list.ToArray();
        for (int i = a.Length - 1; i > 0; --i)
        {
            int j = Random.Range(0, i + 1);
            if (i != j)
            {
                (a[i], a[j]) = (a[j], a[i]);
            }
        }
        return a;
    }

    private static IEnumerable<T> Fraction<T>(IEnumerable<T> list, int section, int partition, bool reverse = false)
    {
        if (reverse) list = list.Reverse();
        int cnt = list.Count();
        if (partition > cnt)
        {
            return list.Where((x, i) => i == Mathf.FloorToInt(cnt * section / (float)partition));
        }
        else
        {
            float binSize = cnt / (float)partition;
            return list.Where((x, i) => Mathf.FloorToInt(i / (float)binSize) == section);
        }
    }

    private static IEnumerable<T> Range<T>(IEnumerable<T> list, int start, int step, bool reverse = false)
    {
        if (reverse) list = list.Reverse();
        if (step == 0) return list.Where((x, i) => i == start);
        else return list.Where((x, i) => i >= start && (i - start) % step == 0);
    }

    public static int Intervals<T>(IEnumerable<T> list)
    {
        return Mathf.Max(list.Count() - 1, 1);
    }
}
