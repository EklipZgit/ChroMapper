using System;
using System.Collections.Generic;
using Beatmap.Base;

/// <summary>
///     Maps authoritative GLS event references to their box and event indexes.
/// </summary>
/// <remarks>
///     Used by paint-properties, mirror, time-shift, and lane-shift operations after they clone an owning GLS group.
///     It is most valuable when a large group has many nodes in a box and the mapper edits many selected nodes,
///     including stacked duplicates. Previously each selected node copied and searched its source box:
///     <c>O(N + S * E)</c>, where <c>N</c> is group clone/setup work, <c>S</c> selected nodes, and <c>E</c> nodes per
///     box. Building this data-only reference index once makes source resolution <c>O(N + S)</c>.
/// </remarks>
internal sealed class GLSEventLookupIndex
{
    private readonly Dictionary<BaseGLSEvent, EventLocation> eventLocations;

    // Build one reference index per affected source group so selected nodes retain their exact authored array position.
    public GLSEventLookupIndex(BaseEventBoxGroup group)
    {
        var capacity = 0;
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            capacity += group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents.Count;
        }

        eventLocations = new Dictionary<BaseGLSEvent, EventLocation>(capacity, ReferenceComparer.Instance);
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            var events = group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents;
            for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                var evt = events[eventIndex];
                if (!eventLocations.ContainsKey(evt))
                {
                    eventLocations.Add(evt, new EventLocation(boxIndex, eventIndex));
                }
            }
        }
    }

    // Resolve the counterpart in a cloned group by its authoritative source-array position.
    public bool TryGetCloneEvent(
        BaseGLSEvent sourceEvent,
        BaseEventBoxGroup clonedGroup,
        out EventLocation location,
        out BaseGLSEvent clonedEvent)
    {
        if (!eventLocations.TryGetValue(sourceEvent, out location)
            || location.BoxIndex >= clonedGroup.ReadOnlyBoxes.Count)
        {
            clonedEvent = null;
            return false;
        }

        var clonedEvents = clonedGroup.ReadOnlyBoxes[location.BoxIndex].ReadOnlyEvents;
        if (location.EventIndex >= clonedEvents.Count)
        {
            clonedEvent = null;
            return false;
        }

        clonedEvent = clonedEvents[location.EventIndex];
        return true;
    }

    // Materialize selected GLS nodes by owning group once for callers that will clone each parent group.
    public static Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>> GroupSelectedEvents(
        IEnumerable<BaseObject> selectedObjects)
    {
        var groups = new Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>>();
        foreach (var selectedObject in selectedObjects)
        {
            if (selectedObject is not BaseGLSEvent selectedEvent
                || selectedEvent.EventBoxGroupData == null)
            {
                continue;
            }

            var group = selectedEvent.EventBoxGroupData;
            if (!groups.TryGetValue(group, out var groupEvents))
            {
                groupEvents = new List<BaseGLSEvent>();
                groups.Add(group, groupEvents);
            }

            groupEvents.Add(selectedEvent);
        }

        return groups;
    }

    public readonly struct EventLocation
    {
        public EventLocation(int boxIndex, int eventIndex)
        {
            BoxIndex = boxIndex;
            EventIndex = eventIndex;
        }

        public int BoxIndex { get; }

        public int EventIndex { get; }
    }

    // Reference equality prevents equal-looking stacked nodes from overwriting each other's source-array location.
    private sealed class ReferenceComparer : IEqualityComparer<BaseGLSEvent>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(BaseGLSEvent left, BaseGLSEvent right) => ReferenceEquals(left, right);

        public int GetHashCode(BaseGLSEvent obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

/// <summary>
///     Queues replacement GLS nodes by stable identity while a parent group is replaced.
/// </summary>
/// <remarks>
///     Used when an active GLS parent group is replaced after node edits, placement/deletion, paint, mirror, or shifts.
///     It has the largest impact for a large active group with many selected or stacked same-time nodes. Duplicate
///     nodes consume one replacement each, preserving selection cardinality. Both exact and lane-independent
///     rebinding use shared candidate positions, so total lookup work remains <c>O(S + R)</c> across the group.
/// </remarks>
internal sealed class GLSEventReplacementLookup
{
    private readonly Dictionary<NodeIdentity, Queue<int>> replacements = new();
    private readonly Dictionary<LaneIndependentIdentity, LaneIndependentBucket> replacementsIgnoringLane = new();
    private readonly BaseGLSEvent[] replacementEvents;
    private readonly LaneIndependentBucket[] candidateBuckets;
    private readonly bool[] consumed;

    public GLSEventReplacementLookup(IReadOnlyList<BaseGLSEvent> events)
    {
        replacementEvents = new BaseGLSEvent[events.Count];
        candidateBuckets = new LaneIndependentBucket[events.Count];
        consumed = new bool[events.Count];
        for (var index = 0; index < events.Count; index++)
        {
            var replacement = events[index];
            replacementEvents[index] = replacement;
            var identity = new NodeIdentity(replacement);
            if (!replacements.TryGetValue(identity, out var queue))
            {
                queue = new Queue<int>();
                replacements.Add(identity, queue);
            }

            var laneIndependentIdentity = new LaneIndependentIdentity(replacement);
            if (!replacementsIgnoringLane.TryGetValue(laneIndependentIdentity, out var bucket))
            {
                bucket = new LaneIndependentBucket();
                replacementsIgnoringLane.Add(laneIndependentIdentity, bucket);
            }

            candidateBuckets[index] = bucket;
            queue.Enqueue(index);
            bucket.Entries.Enqueue(index);
            bucket.RemainingCount++;
        }
    }

    public bool TryTake(BaseGLSEvent selectedEvent, out BaseGLSEvent replacement)
    {
        if (replacements.TryGetValue(new NodeIdentity(selectedEvent), out var queue)
            && TryConsume(queue, out replacement))
        {
            return true;
        }

        replacement = null;
        return false;
    }

    public bool TryTakeUniqueIgnoringLane(BaseGLSEvent selectedEvent, out BaseGLSEvent replacement)
    {
        if (replacementsIgnoringLane.TryGetValue(new LaneIndependentIdentity(selectedEvent), out var bucket)
            && bucket.RemainingCount == 1
            && TryConsume(bucket.Entries, out replacement))
        {
            return true;
        }

        replacement = null;
        return false;
    }

    // Both indexes can retain stale entries after the other path consumes a replacement; dequeue each
    // stale entry at most once and decrement only the shared bucket's live count.
    private bool TryConsume(Queue<int> queue, out BaseGLSEvent replacement)
    {
        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            if (consumed[index]) continue;

            consumed[index] = true;
            candidateBuckets[index].RemainingCount--;
            replacement = replacementEvents[index];
            return true;
        }

        replacement = null;
        return false;
    }

    private sealed class LaneIndependentBucket
    {
        public readonly Queue<int> Entries = new();

        public int RemainingCount;
    }

    private readonly struct LaneIndependentIdentity : IEquatable<LaneIndependentIdentity>
    {
        public LaneIndependentIdentity(BaseGLSEvent evt)
        {
            Type = evt.GetType();
            RelativeJsonTime = evt.RelativeJsonTime;
        }

        private Type Type { get; }

        private float RelativeJsonTime { get; }

        public bool Equals(LaneIndependentIdentity other) =>
            Type == other.Type && RelativeJsonTime.Equals(other.RelativeJsonTime);

        public override bool Equals(object obj) => obj is LaneIndependentIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ RelativeJsonTime.GetHashCode();
            }
        }
    }

    private readonly struct NodeIdentity : IEquatable<NodeIdentity>
    {
        public NodeIdentity(BaseGLSEvent evt)
        {
            Type = evt.GetType();
            BoxIndex = evt.BoxIndex;
            RelativeJsonTime = evt.RelativeJsonTime;
        }

        internal Type Type { get; }

        internal int BoxIndex { get; }

        internal float RelativeJsonTime { get; }

        public bool Equals(NodeIdentity other) =>
            Type == other.Type
            && BoxIndex == other.BoxIndex
            && RelativeJsonTime.Equals(other.RelativeJsonTime);

        public override bool Equals(object obj) => obj is NodeIdentity other && Equals(other);

        public override int GetHashCode()
        {
            // Keep the identity hash compatible with Unity profiles that do not expose HashCode.Combine.
            unchecked
            {
                var hashCode = Type != null ? Type.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ BoxIndex;
                hashCode = (hashCode * 397) ^ RelativeJsonTime.GetHashCode();
                return hashCode;
            }
        }
    }
}
