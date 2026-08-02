using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
///     Indexes active GLS color-ribbon source intervals by song time.
/// </summary>
/// <remarks>
///     A ribbon starts at a color source and ends at its following interpolated color event. Grid pool refreshes need
///     only the ribbons crossing the lower viewport boundary; scanning every color-filter timeline during scrolling was
///     proportional to the entire map. This augmented treap keeps each source interval's subtree maximum end time,
///     allowing a boundary query to prune branches that cannot overlap and return only visible-crossing ribbon sources.
///     <para>
///     <see cref="GLSEventCommon"/> owns timeline matching and calls <see cref="ReplaceSequence"/> whenever an edited
///     filter sequence is rewired. This class deliberately stores no Unity objects and owns no beatmap mutation.
///     </para>
///     <para>
///     Do not store these intervals on GLS preview nodes: previews are pooled Unity visuals and are deliberately
///     recycled when their source leaves the viewport. The index must outlive those visuals so it can determine that
///     an offscreen source still has a ribbon crossing the boundary and therefore needs its preview recreated.
///     </para>
/// </remarks>
internal sealed class GLSColorTransitionIntervalIndex
{
    // Resolve source replacement/removal without searching the time tree a second time.
    private readonly Dictionary<BaseLightColorBase, TransitionInterval> intervalsBySource = new();
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

    /// <summary>
    ///     Replaces indexed intervals for one rewired filter sequence.
    /// </summary>
    /// <param name="events">Every event identity owned by the sequence, including sources whose link was removed.</param>
    /// <param name="followingEvents">The sequence's newly computed source-to-following-event links.</param>
    public void ReplaceSequence(
        IList<BaseLightColorBase> events,
        IDictionary<BaseLightColorBase, BaseLightColorBase> followingEvents)
    {
        for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
        {
            Remove(events[eventIndex]);
        }

        foreach (var followingEvent in followingEvents)
        {
            var transition = followingEvent.Value;
            if (transition.UsePrevious == 0 && transition.Easing != (int)EaseType.None)
            {
                Add(followingEvent.Key, transition);
            }
        }
    }

    /// <summary>
    ///     Appends sources whose interval satisfies <c>sourceTime &lt; boundary &lt;= transitionEndTime</c>.
    /// </summary>
    /// <remarks>
    ///     Callers clear and reuse their result collection. This method allocates nothing on the refresh path.
    /// </remarks>
    public void GetSourcesAt(float boundary, ICollection<BaseLightColorBase> sources)
    {
        GetSourcesAt(root, boundary, sources);
    }

    private void Add(BaseLightColorBase source, BaseLightColorBase transition)
    {
        Remove(source);
        var interval = new TransitionInterval(source, transition.SongBpmTime, ++nextId);
        intervalsBySource.Add(source, interval);
        root = Insert(root, new IntervalNode(interval, NextPriority()));
    }

    private void Remove(BaseLightColorBase source)
    {
        if (!intervalsBySource.TryGetValue(source, out var interval))
        {
            return;
        }

        intervalsBySource.Remove(source);
        root = Remove(root, interval);
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
        ICollection<BaseLightColorBase> sources)
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
        public TransitionInterval(BaseLightColorBase source, float end, long id)
        {
            Source = source;
            Start = source.SongBpmTime;
            End = end;
            Id = id;
        }

        public BaseLightColorBase Source { get; }
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
