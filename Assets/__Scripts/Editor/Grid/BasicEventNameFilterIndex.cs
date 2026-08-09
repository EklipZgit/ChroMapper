using System;
using System.Collections.Generic;
using Beatmap.Base;

/// <summary>
///     Maintains sorted, reference-counted basic-event name filters for propagation-off label lanes.
/// </summary>
/// <remarks>
///     Before this index, every label refresh scanned all <c>E</c> authored basic events and allocated a dictionary and
///     sorted set per type: <c>O(E + D + L)</c>, where <c>D</c> is visible track definitions and <c>L</c> is displayed
///     filter lanes. A local placement, deletion, selection move, or mirror therefore paid for the entire map. The
///     initial build after the backing map list changes remains <c>O(E * log(F))</c>, where <c>F</c> is filters on an
///     event type. Afterwards add/remove/replace updates cost <c>O(log(F))</c> per affected named event and rendering
///     costs <c>O(D + L)</c>. Counts retain one lane while duplicate events share a filter; the sorted dictionary
///     preserves alphabetical lane order without sorting during refresh.
/// </remarks>
public sealed class BasicEventNameFilterIndex
{
    private readonly Dictionary<int, SortedDictionary<string, int>> filtersByType = new();
    private List<BaseEvent> source;

    public bool EnsureFor(List<BaseEvent> events)
    {
        if (ReferenceEquals(source, events))
        {
            return false;
        }

        // A map reload replaces the authoritative list without collection callbacks, so rebuild once for that new owner.
        source = events;
        filtersByType.Clear();
        for (var index = 0; index < events.Count; index++)
        {
            Add(events[index]);
        }

        return true;
    }

    public void Add(BaseEvent evt)
    {
        var nameFilter = evt.CustomNameFilter;
        if (string.IsNullOrEmpty(nameFilter))
        {
            return;
        }

        // Allocate a sorted dictionary only when this type first receives a visible name filter.
        if (!filtersByType.TryGetValue(evt.Type, out var filters))
        {
            filters = new SortedDictionary<string, int>(StringComparer.Ordinal);
            filtersByType.Add(evt.Type, filters);
        }

        filters.TryGetValue(nameFilter, out var count);
        filters[nameFilter] = count + 1;
    }

    public void Remove(BaseEvent evt)
    {
        var nameFilter = evt.CustomNameFilter;
        if (string.IsNullOrEmpty(nameFilter)
            || !filtersByType.TryGetValue(evt.Type, out var filters)
            || !filters.TryGetValue(nameFilter, out var count))
        {
            return;
        }

        // Keep the lane until its last matching authored event is removed.
        if (count > 1)
        {
            filters[nameFilter] = count - 1;
            return;
        }

        filters.Remove(nameFilter);
        if (filters.Count == 0)
        {
            filtersByType.Remove(evt.Type);
        }
    }

    public bool TryGetFilters(int eventType, out SortedDictionary<string, int> filters) =>
        filtersByType.TryGetValue(eventType, out filters);
}
