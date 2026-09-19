using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// Data-only, per-light color schedule for a single GLS group ID.
/// Rebuilds the same per-light group-state and event-state chains that EventGroupEffect +
/// LightColorGroupEffect produce for playback, kept in the same StateChunksContainer
/// buckets, so light and ribbon previews always match. Similar to how the ring event shit works.
/// Makes it so the ribbon previews don't need to duplicate all the wave / easing / transition / distribution logic.
/// </summary>
public sealed class GLSColorTimeline
{
    private const float MaxBucketBeat = 100000f;

    private readonly BaseDifficulty map;
    private readonly StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>[]
        groupContainers;
    private readonly StateChunksContainer<LightColorEventStateData, BaseLightColorBase>[]
        eventContainers;
    private readonly LightColorEventStateData[] endSentinels;
    private readonly HashSet<BaseLightColorBase> sentinelBases = new();
    private readonly HashSet<BaseLightColorBase> startSentinelBases = new();
    private readonly Dictionary<(BaseLightColorBase Source, int Light), LightColorEventStateData>
        outgoing = new();
    private readonly Dictionary<(BaseLightColorBase Target, int Light), LightColorEventStateData>
        incoming = new();
    private readonly Dictionary<BaseLightColorBase, SegmentBounds> boundsBySource = new();
    private readonly HashSet<BaseLightColorBase> changedNodes = new();
    // Displaced claims regenerate their event slices once per EnsureUpdated flush instead of at every
    // claim mutation, so a remove+add replacement never renders an intermediate state.
    private readonly HashSet<LightColorGroupStateData> pendingRegeneration = new();

    public int LightCount { get; }

    // Sentinel segments can extend past the authored span. These are the editable bounds they render inside
    public float HeadBound { get; }
    public float TailBound { get; }

    // Sources = authored nodes that still own at least one finite-next segment.
    // Sentinels and fully preempted or terminal nodes never appear. 
    // Unordered on purpose: backed by the bounds index so Add/Remove never pays a linear removal.
    public IEnumerable<BaseLightColorBase> Sources
    {
        get
        {
            EnsureUpdated();
            return boundsBySource.Keys;
        }
    }

    // Accumulates across mutations until ClearChangedNodes runs after a consume. Contains every
    // source whose outgoing link/state changed and every target whose incoming link changed,
    // including retired identities so interval caches can drop them without rescanning Sources.
    public IReadOnlyCollection<BaseLightColorBase> ChangedNodes
    {
        get
        {
            EnsureUpdated();
            return changedNodes;
        }
    }

    internal void ClearChangedNodes() => changedNodes.Clear();

    /// <summary>
    /// Builds the per-light schedules for one group ID by replaying the same inserts playback
    /// performs, so the initial build and later edits share one code path.
    /// </summary>
    /// <param name="map">Difficulty supplying the json-time to song-bpm-time conversion.</param>
    /// <param name="lightCount">Physical light count of this group ID.</param>
    /// <param name="groups">Only this ID's groups, in the same sorted order playback inserts them.</param>
    public GLSColorTimeline(
        BaseDifficulty map,
        int lightCount,
        IReadOnlyList<BaseLightColorEventBoxGroup> groups)
    {
        this.map = map;
        LightCount = Mathf.Max(lightCount, 0);
        HeadBound = map != null ? (float)map.JsonTimeToSongBpmTime(0f) : 0f;
        var song = BeatSaberSongContainer.Instance;
        var clipLength = song != null && song.LoadedSong != null && song.Info != null
            ? song.LoadedSong.length
            : 0f;
        // Why is time so complicated
        TailBound = clipLength > 0f
            ? song.Info.BeatsPerMinute / 60f * clipLength
            : 0f;
        groupContainers =
            new StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>[LightCount];
        eventContainers =
            new StateChunksContainer<LightColorEventStateData, BaseLightColorBase>[LightCount];
        endSentinels = new LightColorEventStateData[LightCount];
        var maxBeat = EstimateMaxBeat(groups);
        // Authored content can outlive a short or missing clip; tails still cover every authored beat.
        if (maxBeat > TailBound)
        {
            TailBound = maxBeat;
        }
        for (var light = 0; light < LightCount; light++)
        {
            var groupContainer =
                new StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>();
            var eventContainer =
                new StateChunksContainer<LightColorEventStateData, BaseLightColorBase>();
            groupContainer.Resize(maxBeat);
            eventContainer.Resize(maxBeat);

            // Same sentinel pair LightColorGroupEffect.Initialize installs so boundary segments and
            // UsePrevious resolution match playback exactly.
            var startEvent = new LightColorEventStateData(new BaseLightColorBase(), short.MinValue);
            var endEvent = new LightColorEventStateData(
                new BaseLightColorBase { UsePrevious = 1 },
                float.MaxValue);
            startEvent.EndTime = endEvent.StartTime;
            startEvent.Next = endEvent;
            endEvent.Previous = startEvent;
            eventContainer.AddState(startEvent);
            eventContainer.AddState(endEvent);
            endSentinels[light] = endEvent;
            sentinelBases.Add(startEvent.Base);
            sentinelBases.Add(endEvent.Base);
            startSentinelBases.Add(startEvent.Base);

            groupContainer.AddState(CreateGroupSentinel(short.MinValue));
            groupContainer.AddState(CreateGroupSentinel(float.MaxValue));
            groupContainers[light] = groupContainer;
            eventContainers[light] = eventContainer;
        }

        if (groups != null)
        {
            foreach (var group in groups)
            {
                AddGroup(group);
            }
        }

        EnsureUpdated();
        changedNodes.Clear();
    }

    /// <summary>
    /// Returns this light's state for an authored source. The state's Next is the authoritative
    /// per-light successor state (which may still be a UsePrevious node or, when no real node
    /// follows, the end sentinel identifiable by StartTime == float.MaxValue); StartTime/EndTime
    /// are in SongBpmTime.
    /// </summary>
    public bool TryGetOutgoing(BaseLightColorBase source, int light, out LightColorEventStateData state)
    {
        EnsureUpdated();
        state = null;
        return source != null
            && light >= 0
            && light < LightCount
            && outgoing.TryGetValue((source, light), out state);
    }

    public bool TryGetOutgoingAtGroupTime(BaseLightColorBase representative, int light, out LightColorEventStateData state)
    {
        EnsureUpdated();
        state = null;
        if (representative == null || light < 0 || light >= LightCount
            || representative.EventBoxGroupData is not BaseLightColorEventBoxGroup group)
        {
            return false;
        }
        var claim = groupContainers[light].GetStateFrom(group, null);
        if (claim == null)
            return false;

        var events = (LightColorEventStateData[])claim.Events;
        var relativeTime = representative.RelativeJsonTime;
        var index = events.AsSpan().LowerBoundBy(relativeTime, value => value.Base.RelativeJsonTime);
        if (index >= events.Length || events[index].Base.RelativeJsonTime != relativeTime)
            return false;
        state = events[index];
        return true;
    }

    /// <summary>
    /// Returns the authored state whose Next.Base is the given target on this light. First
    /// serialized overlap and later group preemption are already baked into the per-light chains,
    /// so clipped-away children never appear as incoming links.
    /// </summary>
    public bool TryGetIncoming(
        BaseLightColorBase target,
        int light,
        out LightColorEventStateData previous)
    {
        EnsureUpdated();
        previous = null;
        return target != null
            && light >= 0
            && light < LightCount
            && incoming.TryGetValue((target, light), out previous)
            && !sentinelBases.Contains(previous.Base);
    }

    /// <summary>
    /// Unions the source's actual per-light segments: min StartTime to max EndTime across lights
    /// whose next node is finite (Instant successors included. A following sentinel contributes
    /// nothing and never renders).
    /// </summary>
    public bool TryGetBounds(BaseLightColorBase source, out float start, out float end)
    {
        EnsureUpdated();
        if (source != null && boundsBySource.TryGetValue(source, out var bounds))
        {
            start = bounds.Start;
            end = bounds.End;
            return true;
        }

        start = 0f;
        end = 0f;
        return false;
    }

    /// <summary>
    /// Inserts one group's claims like a fresh EventGroupEffect.InsertData: the first serialized
    /// box wins each (axis, element), eventless boxes still claim, and each inserted state reclips
    /// the previous claimant to this state's distributed start while the following claimant bounds
    /// the new state. Only the affected claimants' events regenerate.
    /// </summary>
    // Claim mutations apply eagerly so later mutations see accurate state, but event regeneration
    // and bounds/lookup rebuilds defer to the next EnsureUpdated. A remove+add replacement or a bulk
    // paste therefore coalesces into one regeneration pass instead of rendering the remove first.
    public void AddGroup(BaseLightColorEventBoxGroup group)
    {
        if (map == null || group == null)
        {
            return;
        }

        InsertGroupClaims(group);
    }

    /// <summary>
    /// Removes the states this group's boxes claimed, regenerating each displaced previous
    /// claimant up to the following claimant's distributed start. Call with the same instance
    /// (and pre-edit box contents) that was added, matching StateManager.RemoveData's
    /// reference/original contract. A group that claimed no light is skipped instead of throwing.
    /// </summary>
    public void RemoveGroup(BaseLightColorEventBoxGroup group)
    {
        if (map == null || group == null)
        {
            return;
        }

        RemoveGroupClaims(group);
    }

    // Flushes deferred event regeneration and refreshes bounds and transition lookups for every node
    // touched since the last flush. Runs once per mutation batch, at the first read or cache consume.
    public void EnsureUpdated()
    {
        if (pendingRegeneration.Count > 0)
        {
            foreach (var state in pendingRegeneration)
            {
                // The bound is the final next claimant's local start, identical to the immediate path.
                var container = groupContainers[state.ElementID];
                RegenerateEvents(state.ElementID, state, container.GetNextStateFrom(state).LocalJsonTime);
            }

            pendingRegeneration.Clear();
        }

        if (changedNodes.Count > 0)
        {
            RefreshChangedBounds();
        }
    }

    /// <summary>
    /// Configures the tween for a timeline state exactly like playback does; only the default-color
    /// source differs (EventAppearanceSO + boost query here versus the production ColorScheme).
    /// Resolves both endpoints up front so the shared preparation allocates no delegates.
    /// </summary>
    public void ConfigureTween(
        LightColorTween tween,
        LightColorEventStateData state,
        EventAppearanceSO appearance,
        Func<float, bool> isBoostAt)
    {
        var start = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        var end = (LightColorEventStateData)(state.Next.UsePrevious ? start : state.Next);
        // resolve base color once
        var startBase = ResolveBaseColor(start.Base, appearance, isBoostAt);
        var endBase = ReferenceEquals(end, start)
            ? startBase
            : ResolveBaseColor(end.Base, appearance, isBoostAt);
        LightColorGroupEffect.ConfigureTween(
            tween,
            state,
            GLSColorDistribution.ApplyNormal(
                startBase, start.Box, start.Base, start.DistributionProgress, start.AffectedLightProgress),
            GLSColorDistribution.ApplyNormal(
                endBase, end.Box, end.Base, end.DistributionProgress, end.AffectedLightProgress),
            GLSColorDistribution.ApplyStrobe(
                startBase, start.Box, start.Base, start.DistributionProgress, start.AffectedLightProgress),
            GLSColorDistribution.ApplyStrobe(
                endBase, end.Box, end.Base, end.DistributionProgress, end.AffectedLightProgress),
            map);
    }

    private static Color ResolveBaseColor(
        BaseLightColorBase evt,
        EventAppearanceSO appearance,
        Func<float, bool> isBoostAt) =>
        GLSEventCommon.GetBaseColor(
            evt,
            isBoostAt != null && isBoostAt(evt.JsonTime),
            appearance);

    private static LightColorGroupStateData CreateGroupSentinel(float time)
    {
        var sentinel = new LightColorGroupStateData(new BaseLightColorEventBoxGroup
        {
            songBpmTime = time,
            JsonTime = time,
        })
        {
            Box = new BaseLightColorEventBox
            {
                IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                Events = Array.Empty<BaseLightColorBase>(),
            },
            LocalJsonTime = time,
            StartTime = time,
        };
        return sentinel;
    }

    private float EstimateMaxBeat(IReadOnlyList<BaseLightColorEventBoxGroup> groups)
    {
        var maxJson = 0f;
        if (groups != null)
        {
            foreach (var group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var box in group.Boxes)
                {
                    if (box == null || box.IsAutomaticAxisLane)
                    {
                        continue;
                    }

                    var lastRelative = box.Events is { Length: > 0 }
                        ? box.Events[^1].RelativeJsonTime
                        : 0f;
                    maxJson = Mathf.Max(
                        maxJson,
                        group.JsonTime + lastRelative
                            + (box.BeatDistribution * Mathf.Max(LightCount, 1)));
                }
            }
        }

        var maxBeat = map != null
            ? (float)map.JsonTimeToSongBpmTime(maxJson)
            : maxJson;
        return Mathf.Clamp(maxBeat, 1f, MaxBucketBeat);
    }

    /// <summary>
    /// Returns the segment owner whose interval ends at the given target on this light, including the
    /// pre-map sentinel so callers can render a lit fade-in before the first authored node.
    /// </summary>
    public bool TryGetIncomingSegment(
        BaseLightColorBase target,
        int light,
        out LightColorEventStateData previous)
    {
        EnsureUpdated();
        previous = null;
        return target != null
            && light >= 0
            && light < LightCount
            && incoming.TryGetValue((target, light), out previous);
    }

    public bool TryGetIncomingAtGroupTime(
        BaseLightColorBase representative,
        int light,
        out LightColorEventStateData segment)
    {
        segment = null;
        return TryGetOutgoingAtGroupTime(representative, light, out var state)
            && incoming.TryGetValue((state.Base, light), out segment);
    }

    /// <summary>True when this segment's resolved start is the pre-map sentinel.</summary>
    public bool IsStartSegment(LightColorEventStateData state)
    {
        var resolved = state != null && state.UsePrevious
            ? (LightColorEventStateData)state.Previous
            : state;
        return resolved != null && startSentinelBases.Contains(resolved.Base);
    }

    // Sentinel heads only produce light when the segment's real endpoint is a lit transition into a
    // lit node; an instant or dark first node stays black for the whole pre-node span.
    internal static bool IsLitHeadSegment(LightColorEventStateData segment) =>
        segment != null
        && segment.Next is LightColorEventStateData next
        && !next.UsePrevious
        && next.EaseType != EaseType.None
        && IsLit(next);

    internal static bool IsLit(LightColorEventStateData state) =>
        state != null
        && (state.Brightness > 0f
            || (state.Base.StrobeBrightness > 0f && GLSEventCommon.GetStrobeFrequency(state.Base) > 0f));

    // Mirrors EventGroupEffect.InsertData: within one group the first serialized box claims each
    // (axis, element) pair, empty authored boxes still claim their lanes, and every claim becomes a
    // per-light group state that later groups preempt at their distributed start.
    private void InsertGroupClaims(BaseLightColorEventBoxGroup group)
    {
        var taken = new HashSet<(Axis Axis, int Element)>();
        foreach (var box in group.Boxes)
        {
            if (box.IsAutomaticAxisLane)
            {
                continue;
            }

            var indexFilter = box.IndexFilter == null
                ? null
                : IndexFilterHelper.Convert(box.IndexFilter, LightCount);
            if (indexFilter == null)
            {
                continue;
            }

            var lastEventTime = box.Events is { Length: > 0 }
                ? box.Events[^1].RelativeJsonTime
                : 0f;
            var beatStep = DistributionHelper.GetBeatStep(
                DistributionHelper.GetDurationCount(indexFilter),
                (DistributionType)box.BeatDistributionType,
                box.BeatDistribution,
                lastEventTime);
            foreach (var entry in indexFilter)
            {
                var (element, durationOrder, distributionOrder) = entry;
                var key = (box.GetAxis(), element);
                if (!taken.Add(key) || element < 0 || element >= LightCount)
                {
                    continue;
                }

                var state = new LightColorGroupStateData(group)
                {
                    StartTime = group.SongBpmTime,
                    LocalJsonTime = group.JsonTime + (beatStep * durationOrder),
                    BeatStep = beatStep,
                    Box = box,
                    ElementID = element,
                    DurationOrder = durationOrder,
                    DistributionOrder = distributionOrder,
                    AffectedChunkOrder = entry.AffectedChunkOrder,
                    AffectedChunkCount = indexFilter.VisibleCount,
                    AffectedLightOrder = entry.AffectedLightOrder,
                    AffectedLightCount = indexFilter.AffectedLightCount,
                    ConvertedIndexFilter = indexFilter,
                };
                InsertGroupState(state);
            }
        }
    }

    // Mirrors EventGroupEffect.RemoveData's traversal: the same claim keys are recomputed from the
    // original boxes so a removed group releases exactly the elements it owns.
    private void RemoveGroupClaims(BaseLightColorEventBoxGroup group)
    {
        var taken = new HashSet<(Axis Axis, int Element)>();
        foreach (var box in group.Boxes)
        {
            if (box.IsAutomaticAxisLane)
            {
                continue;
            }

            var indexFilter = box.IndexFilter == null
                ? null
                : IndexFilterHelper.Convert(box.IndexFilter, LightCount);
            if (indexFilter == null)
            {
                continue;
            }

            foreach (var (element, _, _) in indexFilter)
            {
                var key = (box.GetAxis(), element);
                if (!taken.Add(key) || element < 0 || element >= LightCount)
                {
                    continue;
                }

                var state = groupContainers[element].GetStateFrom(group, group);
                if (state == null)
                {
                    continue;
                }

                RemoveGroupState(state);
            }
        }
    }

    // Same flow as EventGroupEffect's insert: the previous claimant is reclipped to the new state's
    // distributed start and the new state is bounded by the following claimant's distributed start.
    private void InsertGroupState(LightColorGroupStateData newState)
    {
        var element = newState.ElementID;
        var groupContainer = groupContainers[element];
        var prevState = groupContainer.GetOverlappingStateFrom(newState);
        var nextState = groupContainer.GetNextStateFrom(newState);
        prevState.EndTime = newState.StartTime;

        // Event slices defer to the next flush so each displaced claim regenerates once per batch.
        RemoveEvents(element, prevState);
        pendingRegeneration.Add(prevState);
        pendingRegeneration.Add(newState);

        newState.EndTime = nextState.StartTime;
        groupContainer.AddState(newState);
    }

    // Same flow as EventGroupEffect's remove: the previous claimant absorbs the removed span and is
    // regenerated up to the following claimant's distributed start.
    private void RemoveGroupState(LightColorGroupStateData currState)
    {
        var element = currState.ElementID;
        var groupContainer = groupContainers[element];
        var prevState = groupContainer.GetPreviousStateFrom(currState);
        var nextState = groupContainer.GetNextStateFrom(currState);
        prevState.EndTime = nextState.StartTime;

        RemoveEvents(element, prevState);
        RemoveEvents(element, currState);
        pendingRegeneration.Add(prevState);
        // A state dirtied by an earlier mutation in the same batch must not regenerate after removal.
        pendingRegeneration.Remove(currState);
        groupContainer.RemoveState(currState);
    }

    private void RemoveEvents(int element, LightColorGroupStateData state)
    {
        var eventContainer = eventContainers[element];
        foreach (var evt in state.Events)
        {
            RemoveEventState(element, eventContainer, (LightColorEventStateData)evt);
        }

        // Deferred regeneration must not replay or double-remove stale events.
        state.Events = Array.Empty<LightColorEventStateData>();
    }

    private void RegenerateEvents(
        int element,
        LightColorGroupStateData state,
        float maxRelativeJsonTime)
    {
        var indexFilter = state.Box.IndexFilter == null || state.Box.Events == null
            ? null
            : state.ConvertedIndexFilter ?? IndexFilterHelper.Convert(state.Box.IndexFilter, LightCount);
        var generated = indexFilter == null
            ? Array.Empty<LightColorEventStateData>()
            : LightColorGroupEffect.GenerateColorEvents(
                map,
                state,
                LightColorGroupEffect.GetBrightnessStep(indexFilter, state.Box, state.DistributionOrder),
                maxRelativeJsonTime);
        var eventContainer = eventContainers[element];
        foreach (var data in generated)
        {
            InsertEventState(element, eventContainer, data);
        }

        state.Events = generated;
    }

    private void InsertEventState(
        int element,
        StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer,
        LightColorEventStateData newState)
    {
        LightColorGroupEffect.HandleInsertEventState(
            eventContainer,
            newState,
            out var prevState,
            out var nextState);

        outgoing[(newState.Base, element)] = newState;
        incoming[(newState.Base, element)] = prevState;
        if (outgoing.ContainsKey((nextState.Base, element)))
        {
            incoming[(nextState.Base, element)] = newState;
        }

        TrackChange(newState, prevState, nextState);
    }

    private void RemoveEventState(
        int element,
        StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer,
        LightColorEventStateData stateToRemove)
    {
        LightColorGroupEffect.HandleRemoveEventState(
            eventContainer,
            stateToRemove,
            out var prevState,
            out var nextState);

        outgoing.Remove((stateToRemove.Base, element));
        incoming.Remove((stateToRemove.Base, element));
        if (outgoing.ContainsKey((nextState.Base, element)))
        {
            incoming[(nextState.Base, element)] = prevState;
        }

        TrackChange(stateToRemove, prevState, nextState);
    }

    private void RefreshChangedBounds()
    {
        foreach (var node in changedNodes)
            RecomputeBounds(node);
    }

    // Bounds are a union over the source's per-light segments, so inserts can only grow them and
    // removals or predecessor relinks must rebuild them; the rebuild is O(LightCount) dictionary
    // probes instead of a chain scan. Sentinel bases have no outgoing entries and no-op here.
    private void RecomputeBounds(BaseLightColorBase source)
    {
        var start = float.MaxValue;
        var end = float.MinValue;
        var any = false;
        for (var light = 0; light < LightCount; light++)
        {
            if (outgoing.TryGetValue((source, light), out var state))
            {
                var next = state.Next;
                if (next != null)
                {
                    if (ReferenceEquals(next, endSentinels[light]))
                    {
                        var held = state.UsePrevious
                            ? (LightColorEventStateData)state.Previous
                            : state;
                        if (IsLit(held) && TailBound > state.StartTime)
                        {
                            start = Mathf.Min(start, state.StartTime);
                            end = Mathf.Max(end, TailBound);
                            any = true;
                        }
                    }
                    else
                    {
                        start = Mathf.Min(start, state.StartTime);
                        end = Mathf.Max(end, state.EndTime);
                        any = true;
                    }
                }
            }

            if (incoming.TryGetValue((source, light), out var previous)
                && IsStartSegment(previous)
                && previous.EndTime > HeadBound
                && IsLitHeadSegment(previous))
            {
                start = Mathf.Min(start, HeadBound);
                end = Mathf.Max(end, previous.EndTime);
                any = true;
            }
        }

        if (any)
        {
            boundsBySource[source] = new SegmentBounds { Start = start, End = end };
        }
        else
        {
            boundsBySource.Remove(source);
        }
    }

    private void TrackChange(
        LightColorEventStateData state,
        LightColorEventStateData prevState,
        LightColorEventStateData nextState)
    {
        if (!sentinelBases.Contains(state.Base))
        {
            changedNodes.Add(state.Base);
        }

        if (!sentinelBases.Contains(prevState.Base))
        {
            changedNodes.Add(prevState.Base);
        }

        if (!sentinelBases.Contains(nextState.Base))
        {
            changedNodes.Add(nextState.Base);
        }
    }

    private struct SegmentBounds
    {
        public float Start;
        public float End;
    }
}
