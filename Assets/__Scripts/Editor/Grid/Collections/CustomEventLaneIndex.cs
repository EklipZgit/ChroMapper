using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Helper;
using SimpleJSON;

public sealed class CustomEventLaneIndex
{
    internal const int SectionSpacing = 2;

    public sealed class Section
    {
        public string Type { get; }
        public int StartColumn { get; }
        public int LaneCount { get; }

        internal Section(string type, int startColumn, int laneCount)
        {
            Type = type;
            StartColumn = startColumn;
            LaneCount = laneCount;
        }
    }

    public sealed class Lane
    {
        public string Type { get; }
        public string Label { get; }
        public string DataKey { get; }
        private readonly JSONNode target;
        internal readonly string TargetKey;
        internal readonly string[] Tracks;
        internal int Index;
        internal int EventCount;
        internal int ReferenceCount;
        internal int TotalEventCount;
        public int Column => Index;

        internal Lane(string type, string dataKey, JSONNode target, int index)
        {
            Type = type;
            DataKey = dataKey;
            this.target = target;
            TargetKey = target?.ToString();
            if (target is JSONString name)
                Tracks = new[] { name.Value };
            else if (target is JSONArray names)
            {
                Tracks = new string[names.Count];
                for (var targetIndex = 0; targetIndex < names.Count; targetIndex++)
                    Tracks[targetIndex] = names[targetIndex].Value;
            }

            Index = index;
            var targetLabel = GetTargetLabel(target);
            Label = dataKey == null
                ? type
                : targetLabel;
        }

        private static string GetTargetLabel(JSONNode target)
        {
            if (target is not JSONArray names)
                return target?.Value;

            var labels = new string[names.Count];
            for (var index = 0; index < names.Count; index++)
                labels[index] = names[index].ToString();

            return string.Join(" + ", labels);
        }

        public void ApplyTo(BaseCustomEvent ev)
        {
            ev.Type = Type;
            ev.Data = DataKey == null
                ? new JSONObject()
                : new JSONObject { [DataKey] = target.Clone() };
        }
    }

    private readonly List<Lane> lanes = new();
    private readonly List<Lane> columns = new();
    private readonly List<Section> sections = new();
    private readonly Dictionary<string, Lane> typeLanes = new(StringComparer.Ordinal);
    private readonly List<Lane> typeOrder = new();
    private readonly Dictionary<(string Type, string DataKey, string Target), Lane> targetLanes = new();
    private readonly Dictionary<BaseCustomEvent, Lane> eventLanes = new();
    private readonly Dictionary<BaseItem, Lane[]> trackReferences =
        new(ReferenceIdentityComparer<BaseItem>.Instance);
    private bool splitByTrack;
    private bool orderDirty;
    public IReadOnlyList<Section> Sections => sections;

    public Lane GetLaneAtColumn(int column)
    {
        return column >= 0 && column < columns.Count
            ? columns[column]
            : null;
    }

    public IReadOnlyList<Lane> Lanes => lanes;

    public void Rebuild(IEnumerable<string> types, IEnumerable<BaseCustomEvent> events, bool split)
    {
        lanes.Clear();
        columns.Clear();
        sections.Clear();
        typeLanes.Clear();
        typeOrder.Clear();
        targetLanes.Clear();
        eventLanes.Clear();
        trackReferences.Clear();
        splitByTrack = split;
        orderDirty = true;
        foreach (var type in types)
            AddType(type);

        if (split)
        {
            AddType("AnimateTrack");
            foreach (var ev in events)
            {
                Add(ev);
                AddTrackReferences(ev);
            }
        }

        SortLanes();
    }

    public bool AddType(string type)
    {
        if (typeLanes.ContainsKey(type))
            return false;

        var lane = new Lane(type, null, null, lanes.Count);
        lanes.Add(lane);
        typeLanes.Add(type, lane);
        typeOrder.Add(lane);
        orderDirty = true;
        return true;
    }

    public bool Add(BaseCustomEvent ev)
    {
        var changed = AddType(ev.Type);
        if (!splitByTrack || eventLanes.ContainsKey(ev))
            return changed;

        var dataKey = ev.Type == "AssignTrackParent"
            ? ev.DataKeyParentTrack
            : ev.CustomKeyTrack;
        var target = NormalizeTarget(ev.Type == "AssignTrackParent" ? ev.DataParentTrack : ev.CustomTrack);
        var lane = typeLanes[ev.Type];
        if (target != null)
            lane = GetOrCreateTargetLane(ev.Type, dataKey, target, ref changed);

        lane.EventCount++;
        eventLanes.Add(ev, lane);
        var type = typeLanes[ev.Type];
        type.TotalEventCount++;
        if (type.TotalEventCount == 1 || (lane.DataKey == null && lane.EventCount == 1))
        {
            orderDirty = true;
            changed = true;
        }

        return changed;
    }

    private Lane GetOrCreateTargetLane(string type, string dataKey, JSONNode target, ref bool changed)
    {
        var key = (type, dataKey, target.ToString());
        if (targetLanes.TryGetValue(key, out var lane))
            return lane;

        lane = new Lane(type, dataKey, target, lanes.Count);
        targetLanes.Add(key, lane);
        lanes.Add(lane);
        orderDirty = true;
        changed = true;
        return lane;
    }

    public bool AddTrackReferences(BaseObject obj)
    {
        if (!splitByTrack)
            return false;

        var tracks = TrackDataReader.ReadNames(obj.CustomTrack);
        if (obj is not BaseCustomEvent ev)
            return SetTrackReferences(obj, tracks);

        var parents = TrackDataReader.ReadNames(ev.DataParentTrack);
        var children = TrackDataReader.ReadNames(ev.DataChildrenTracks);
        if (parents.Length == 0 && children.Length == 0)
            return SetTrackReferences(obj, tracks);

        var names = new List<string>(tracks.Length + parents.Length + children.Length);
        names.AddRange(tracks);
        names.AddRange(parents);
        names.AddRange(children);

        return SetTrackReferences(obj, names);
    }

    public bool AddTrackReferences(BaseMaterial material) => splitByTrack
        && SetTrackReferences(material, material.TrackNames);

    private bool SetTrackReferences(BaseItem owner, IReadOnlyList<string> names)
    {
        if (trackReferences.TryGetValue(owner, out var existing) && existing.Length == names.Count)
        {
            var matches = true;
            for (var index = 0; index < existing.Length; index++)
                matches &= existing[index].Tracks[0] == names[index];

            if (matches)
                return false;
        }

        var changed = RemoveTrackReferences(owner);
        if (names.Count == 0)
            return changed;

        var references = new Lane[names.Count];
        var trackKey = Settings.Instance.MapVersion == 2
            ? "_track"
            : "track";
        for (var index = 0; index < names.Count; index++)
        {
            var lane = GetOrCreateTargetLane("AnimateTrack", trackKey, new JSONString(names[index]), ref changed);
            lane.ReferenceCount++;
            references[index] = lane;
        }

        trackReferences.Add(owner, references);
        return changed;
    }

    public bool RemoveTrackReferences(BaseItem owner)
    {
        if (!trackReferences.Remove(owner, out var references))
            return false;

        var changed = false;
        foreach (var lane in references)
        {
            lane.ReferenceCount--;
            changed |= RemoveEmptyLane(lane);
        }

        return changed;
    }

    public bool Remove(BaseCustomEvent ev)
    {
        if (!eventLanes.Remove(ev, out var lane))
            return false;

        lane.EventCount--;
        var type = typeLanes[ev.Type];
        type.TotalEventCount--;
        var typeChanged = type.TotalEventCount == 0 || (lane.DataKey == null && lane.EventCount == 0);
        orderDirty |= typeChanged;

        return RemoveEmptyLane(lane) || typeChanged;
    }

    private bool RemoveEmptyLane(Lane lane)
    {
        if (lane.DataKey == null || lane.EventCount != 0 || lane.ReferenceCount != 0)
            return false;

        targetLanes.Remove((lane.Type, lane.DataKey, lane.TargetKey));
        // A replacement or bulk deletion can empty many lanes. Compact them once before refreshing the grid.
        orderDirty = true;
        return true;
    }

    public int GetLaneIndex(BaseCustomEvent ev) => eventLanes.TryGetValue(ev, out var lane)
        ? lane.Index
        : typeLanes.TryGetValue(ev.Type, out lane)
            ? lane.Index
            : -1;

    public void SortLanes()
    {
        if (!orderDirty)
            return;

        orderDirty = false;
        columns.Clear();
        sections.Clear();
        if (!splitByTrack)
        {
            columns.AddRange(lanes);
            return;
        }

        var groupsByType = new Dictionary<string, List<Lane>>(StringComparer.Ordinal);
        var ordered = new List<Lane>(lanes.Count);
        foreach (var lane in targetLanes.Values)
        {
            if (!groupsByType.TryGetValue(lane.Type, out var groups))
            {
                groups = new List<Lane>();
                groupsByType.Add(lane.Type, groups);
            }

            groups.Add(lane);
        }

        foreach (var type in typeOrder)
        {
            if (type.Type == "AnimateTrack")
                continue;

            AppendSection(type);
        }

        AppendSection(typeLanes["AnimateTrack"]);
        lanes.Clear();
        lanes.AddRange(ordered);

        void AppendSection(Lane type)
        {
            var firstLane = ordered.Count;
            type.Index = -1;
            if (type.TotalEventCount == 0 || type.EventCount > 0)
                ordered.Add(type);

            if (groupsByType.TryGetValue(type.Type, out var groups))
                AppendTrackGroups(groups, ordered);

            if (columns.Count > 0)
            {
                for (var gap = 0; gap < SectionSpacing; gap++)
                    columns.Add(null);
            }

            var startColumn = columns.Count;
            for (var index = firstLane; index < ordered.Count; index++)
            {
                var lane = ordered[index];
                lane.Index = columns.Count;
                columns.Add(lane);
            }

            sections.Add(new Section(type.Type, startColumn, ordered.Count - firstLane));
        }
    }

    private static void AppendTrackGroups(List<Lane> lanes, List<Lane> ordered)
    {
        var singles = new Dictionary<string, Lane>(StringComparer.Ordinal);
        var groupsByTrack = new Dictionary<string, List<Lane>>(StringComparer.Ordinal);
        var tracks = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var lane in lanes)
        {
            foreach (var track in lane.Tracks)
                tracks.Add(track);

            if (lane.Tracks.Length == 1)
            {
                singles.Add(lane.Tracks[0], lane);
                continue;
            }

            foreach (var track in lane.Tracks)
            {
                if (!groupsByTrack.TryGetValue(track, out var groups))
                {
                    groups = new List<Lane>();
                    groupsByTrack.Add(track, groups);
                }

                groups.Add(lane);
            }
        }

        foreach (var groups in groupsByTrack.Values)
        {
            groups.Sort((left, right) =>
            {
                var sizeOrder = right.Tracks.Length.CompareTo(left.Tracks.Length);
                return sizeOrder != 0
                    ? sizeOrder
                    : string.CompareOrdinal(left.TargetKey, right.TargetKey);
            });
        }

        var emittedGroups = new HashSet<Lane>();
        var emittedTracks = new HashSet<string>(StringComparer.Ordinal);
        foreach (var track in tracks)
            AppendTrack(track);

        void AppendTrack(string track)
        {
            if (emittedTracks.Contains(track))
                return;

            if (groupsByTrack.TryGetValue(track, out var groups))
            {
                foreach (var group in groups)
                {
                    if (!emittedGroups.Add(group))
                        continue;

                    ordered.Add(group);
                    foreach (var member in group.Tracks)
                        AppendTrack(member);
                }
            }

            if (emittedTracks.Add(track) && singles.TryGetValue(track, out var single))
                ordered.Add(single);
        }
    }

    // Track arrays are sets of targets. Their order and repeated names must not create additional lanes.
    private static JSONNode NormalizeTarget(JSONNode target)
    {
        if (target is JSONString name)
            return new JSONString(name.Value);

        if (target is not JSONArray names || names.Count == 0)
            return null;

        var tracks = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in names.Children)
        {
            if (entry is not JSONString track)
                return null;

            tracks.Add(track.Value);
        }

        if (tracks.Count == 1)
        {
            foreach (var track in tracks)
                return new JSONString(track);
        }

        var result = new JSONArray();
        foreach (var track in tracks)
            result.Add(track);

        return result;
    }
}
