using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Indexes transition source intervals by song time.
/// </summary>
/// <remarks>
///     A rendered transition starts at a source and ends at its following interpolated event. Grid pool refreshes need
///     only transitions crossing the lower viewport boundary; scanning every timeline during scrolling would be
///     proportional to the entire map. This augmented treap keeps each source interval's subtree maximum end time,
///     allowing a boundary query to prune branches that cannot overlap and return only crossing sources.
///     <para>
///     Callers own timeline matching and update only sources whose successor changed. This class stores source-keyed
///     data and owns no beatmap mutation or pooled visual state.
///     </para>
///     <para>
///     Do not store these intervals on preview nodes: previews are pooled visuals and are deliberately recycled when
///     their source leaves the viewport. The index must outlive those visuals so it can determine that an offscreen
///     source still has a transition crossing the boundary and therefore needs its preview recreated.
///     </para>
/// </remarks>
internal sealed class TransitionIntervalIndex<TSource> where TSource : class
{
    // Resolve source replacement/removal without searching the time tree a second time.
    private readonly Dictionary<TSource, TransitionInterval> intervalsBySource = new();
    // The root is ordered by source time; each node caches the farthest transition end below it.
    private IntervalNode root;
    // Equal-time events need a stable, unique secondary key because all can be valid indexed sources.
    private long nextId;
    // A deterministic treap priority keeps insertion/removal balanced without allocating tree-management structures.
    private uint randomState = 2463534242;

    /// <summary>
    ///     Drops all intervals when the active beatmap changes.
    /// </summary>
    public void Clear()
    {
        intervalsBySource.Clear();
        root = null;
        nextId = 0;
    }

    // Replace one source interval without searching or rebuilding unrelated transition lanes.
    public void AddOrReplace(TSource source, float start, float end)
    {
        Remove(source);
        var interval = new TransitionInterval(source, start, end, ++nextId);
        intervalsBySource.Add(source, interval);
        root = Insert(root, new IntervalNode(interval, NextPriority()));
    }

    // A deleted or rewired source no longer owns its former viewport-retention interval.
    public void Remove(TSource source)
    {
        if (!intervalsBySource.TryGetValue(source, out var interval))
        {
            return;
        }

        intervalsBySource.Remove(source);
        root = Remove(root, interval);
    }

    /// <summary>
    ///     Appends sources whose interval satisfies <c>sourceTime &lt; boundary &lt;= transitionEndTime</c>.
    /// </summary>
    /// <remarks>
    ///     Callers clear and reuse their result collection. This method allocates nothing on the refresh path.
    /// </remarks>
    public void GetSourcesAt(float boundary, ICollection<TSource> sources)
    {
        GetSourcesAt(root, boundary, sources);
    }

    private static IntervalNode Insert(IntervalNode node, IntervalNode inserted)
    {
        if (node == null)
        {
            return inserted;
        }

        if (Compare(inserted.Interval, node.Interval) < 0)
        {
            node.Left = Insert(node.Left, inserted);
            if (node.Left.Priority < node.Priority)
            {
                node = RotateRight(node);
            }
        }
        else
        {
            node.Right = Insert(node.Right, inserted);
            if (node.Right.Priority < node.Priority)
            {
                node = RotateLeft(node);
            }
        }

        UpdateMaxEnd(node);
        return node;
    }

    private static IntervalNode Remove(IntervalNode node, TransitionInterval interval)
    {
        if (node == null)
        {
            return null;
        }

        var comparison = Compare(interval, node.Interval);
        if (comparison < 0)
        {
            node.Left = Remove(node.Left, interval);
        }
        else if (comparison > 0)
        {
            node.Right = Remove(node.Right, interval);
        }
        else
        {
            return Merge(node.Left, node.Right);
        }

        UpdateMaxEnd(node);
        return node;
    }

    private static IntervalNode Merge(IntervalNode left, IntervalNode right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        if (left.Priority < right.Priority)
        {
            left.Right = Merge(left.Right, right);
            UpdateMaxEnd(left);
            return left;
        }

        right.Left = Merge(left, right.Left);
        UpdateMaxEnd(right);
        return right;
    }

    private static IntervalNode RotateLeft(IntervalNode node)
    {
        var newRoot = node.Right;
        node.Right = newRoot.Left;
        newRoot.Left = node;
        UpdateMaxEnd(node);
        UpdateMaxEnd(newRoot);
        return newRoot;
    }

    private static IntervalNode RotateRight(IntervalNode node)
    {
        var newRoot = node.Left;
        node.Left = newRoot.Right;
        newRoot.Right = node;
        UpdateMaxEnd(node);
        UpdateMaxEnd(newRoot);
        return newRoot;
    }

    private static void GetSourcesAt(
        IntervalNode node,
        float boundary,
        ICollection<TSource> sources)
    {
        if (node == null || node.MaxEnd < boundary)
        {
            return;
        }

        GetSourcesAt(node.Left, boundary, sources);
        if (node.Interval.Start < boundary && node.Interval.End >= boundary)
        {
            sources.Add(node.Interval.Source);
        }

        if (node.Interval.Start < boundary)
        {
            GetSourcesAt(node.Right, boundary, sources);
        }
    }

    private static int Compare(TransitionInterval left, TransitionInterval right)
    {
        var comparison = left.Start.CompareTo(right.Start);
        return comparison != 0 ? comparison : left.Id.CompareTo(right.Id);
    }

    private static void UpdateMaxEnd(IntervalNode node)
    {
        var maxEnd = node.Interval.End;
        if (node.Left != null)
        {
            maxEnd = Mathf.Max(maxEnd, node.Left.MaxEnd);
        }

        if (node.Right != null)
        {
            maxEnd = Mathf.Max(maxEnd, node.Right.MaxEnd);
        }

        node.MaxEnd = maxEnd;
    }

    private uint NextPriority()
    {
        randomState ^= randomState << 13;
        randomState ^= randomState >> 17;
        randomState ^= randomState << 5;
        return randomState;
    }

    // Immutable interval data remains valid while its treap node is being rotated or merged.
    private readonly struct TransitionInterval
    {
        public TransitionInterval(TSource source, float start, float end, long id)
        {
            Source = source;
            Start = start;
            End = end;
            Id = id;
        }

        public TSource Source { get; }
        public float Start { get; }
        public float End { get; }
        public long Id { get; }
    }

    // Augment each ordered treap node with its subtree's maximum end time for overlap pruning.
    private sealed class IntervalNode
    {
        public IntervalNode(TransitionInterval interval, uint priority)
        {
            Interval = interval;
            Priority = priority;
            MaxEnd = interval.End;
        }

        public TransitionInterval Interval { get; }
        public uint Priority { get; }
        public float MaxEnd { get; set; }
        public IntervalNode Left { get; set; }
        public IntervalNode Right { get; set; }
    }
}
