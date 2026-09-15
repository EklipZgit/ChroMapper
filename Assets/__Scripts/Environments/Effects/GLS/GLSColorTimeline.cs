using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// Data-only, per-light color schedule for a single GLS group ID.
/// Rebuilds the same per-light group-state and event-state chains that EventGroupEffect +
/// LightColorGroupEffect produce for playback — kept in the same StateChunksContainer
/// buckets — so collider/ribbon previews and GPU twins can query authoritative per-light
/// successor links and configure identical tweens without GameObjects, scene state, or
/// per-sample map scans.
///
/// Lifetime: the outer cache keeps one timeline per group ID + light-count revision and
/// applies exact AddGroup/RemoveGroup edits. Each edit regenerates only the changed
/// claimant and the previous claimant's future children up to the next distributed group
/// start; every node whose link or segment changed is reported through ChangedNodes.
/// Construction costs one claim traversal per group plus per-event bucket ops — no
/// whole-chain scans or per-event linear searches.
/// </summary>
public sealed class GLSColorTimeline
{
    // Bucket span is a lookup-granularity hint, not a bound: SortedBucketArray clamps
    // out-of-range times into the edge buckets, so an extreme authored tail cannot allocate
    // millions of empty lists.
    private const float MaxBucketBeat = 100000f;

    private readonly BaseDifficulty map;
    private readonly StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>[]
        groupContainers;
    private readonly StateChunksContainer<LightColorEventStateData, BaseLightColorBase>[]
        eventContainers;
    private readonly LightColorEventStateData[] endSentinels;
    private readonly HashSet<BaseLightColorBase> sentinelBases = new();
    // LitHeadRetainsTargetGroupFromMapStart distinguishes pre-map sentinel segments from end sentinels.
    private readonly HashSet<BaseLightColorBase> startSentinelBases = new();
    private readonly Dictionary<(BaseLightColorBase Source, int Light), LightColorEventStateData>
        outgoing = new();
    private readonly Dictionary<(BaseLightColorBase Target, int Light), LightColorEventStateData>
        incoming = new();
    private readonly Dictionary<BaseLightColorBase, SegmentBounds> boundsBySource = new();
    private readonly HashSet<BaseLightColorBase> changedNodes = new();

    public int LightCount { get; }

    // Sentinel segments can extend past the authored span; these are the editable bounds they render
    // inside. TailBound tracks the loaded song's end so a held tail never extends past playback range.
    public float HeadBound { get; }
    public float TailBound { get; }

    // Sources = authored nodes that still own at least one finite-next segment; sentinels and
    // fully preempted or terminal nodes never appear. Unordered on purpose: backed by the
    // bounds index so Add/Remove never pays a linear removal.
    public IEnumerable<BaseLightColorBase> Sources => boundsBySource.Keys;

    // Reset at the start of each AddGroup/RemoveGroup. Contains every source whose outgoing
    // link/state changed and every target whose incoming link changed, including retired
    // identities so interval caches can drop them without rescanning Sources.
    public IReadOnlyCollection<BaseLightColorBase> ChangedNodes => changedNodes;

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
        // FirstNodeHeadExtendsInnerIncomingRibbonToMapStart: pre-map sentinel segments still produce
        // light, so their visible head starts at map beat zero rather than the authored node.
        HeadBound = map != null ? (float)map.JsonTimeToSongBpmTime(0f) : 0f;
        var song = BeatSaberSongContainer.Instance;
        var clipLength = song != null && song.LoadedSong != null && song.Info != null
            ? song.LoadedSong.length
            : 0f;
        // LitTailExtendsOutgoingRibbonToSongEnd: held tails end at the song's end beat, matching the
        // same bpm/60 * seconds bound LightColorGroupEffect.Initialize uses for its buckets.
        var songEndBeat = clipLength > 0f
            ? song.Info.BeatsPerMinute / 60f * clipLength
            : 0f;
        TailBound = map != null && songEndBeat > 0f
            ? (float)map.JsonTimeToSongBpmTime(songEndBeat)
            : songEndBeat;
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

            // Same empty division-1 sentinel group states; they generate no events while still
            // bounding real claims on this light.
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
        state = null;
        return source != null
            && light >= 0
            && light < LightCount
            && outgoing.TryGetValue((source, light), out state);
    }

    // OuterAlternatingChunkRibbonsIncludeBothBoxes keeps one body per timestamp but resolves each strip through its winning box.
    public bool TryGetOutgoingAtGroupTime(BaseLightColorBase representative, int light, out LightColorEventStateData state)
    {
        state = null;
        if (representative == null || light < 0 || light >= LightCount
            || representative.EventBoxGroupData is not BaseLightColorEventBoxGroup group)
        {
            return false;
        }
        var claim = groupContainers[light].GetStateFrom(group, null);
        if (claim == null)
            return false;
        // Color generation owns a derived array; Span over the covariant base-array view would throw ArrayTypeMismatchException.
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
        previous = null;
        // FirstAuthoredNodeHasNoSentinelIncomingRibbon keeps the internal initial state out of visible incoming intervals.
        return target != null
            && light >= 0
            && light < LightCount
            && incoming.TryGetValue((target, light), out previous)
            && !sentinelBases.Contains(previous.Base);
    }

    /// <summary>
    /// Unions the source's actual per-light segments: min StartTime to max EndTime across lights
    /// whose next node is finite (Instant successors included; a following sentinel contributes
    /// nothing and never renders).
    /// </summary>
    public bool TryGetBounds(BaseLightColorBase source, out float start, out float end)
    {
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
    public void AddGroup(BaseLightColorEventBoxGroup group)
    {
        changedNodes.Clear();
        if (map == null || group == null)
        {
            return;
        }

        InsertGroupClaims(group);
        // Recompute each changed node's light union once, not once for every physical-light insertion.
        RefreshChangedBounds();
    }

    /// <summary>
    /// Removes the states this group's boxes claimed, regenerating each displaced previous
    /// claimant up to the following claimant's distributed start. Call with the same instance
    /// (and pre-edit box contents) that was added, matching StateManager.RemoveData's
    /// reference/original contract; a group that claimed no light is skipped instead of throwing.
    /// </summary>
    public void RemoveGroup(BaseLightColorEventBoxGroup group)
    {
        changedNodes.Clear();
        if (map == null || group == null)
        {
            return;
        }

        RemoveGroupClaims(group);
        // Removal can affect the same source on many lights; deduplicate its bound refresh across the completed edit.
        RefreshChangedBounds();
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
        // Held segments resolve both endpoints to the same state; resolve its base color once.
        var startBase = ResolveBaseColor(start.Base, appearance, isBoostAt);
        var endBase = ReferenceEquals(end, start)
            ? startBase
            : ResolveBaseColor(end.Base, appearance, isBoostAt);
        LightColorGroupEffect.ConfigureTween(
            tween,
            state,
            GLSColorShift.ApplyNormal(
                startBase, start.Box, start.Base, start.DistributionProgress, start.AffectedLightProgress),
            GLSColorShift.ApplyNormal(
                endBase, end.Box, end.Base, end.DistributionProgress, end.AffectedLightProgress),
            GLSColorShift.ApplyStrobe(
                startBase, start.Box, start.Base, start.DistributionProgress, start.AffectedLightProgress),
            GLSColorShift.ApplyStrobe(
                endBase, end.Box, end.Base, end.DistributionProgress, end.AffectedLightProgress));
    }

    // GLSEventCommon owns the boost-aware default/custom color table; the timeline evaluates boost
    // at the endpoint's own authored beat, matching the node's preview semantics.
    private static Color ResolveBaseColor(
        BaseLightColorBase evt,
        EventAppearanceSO appearance,
        Func<float, bool> isBoostAt) =>
        GLSEventCommon.GetBaseColor(
            evt,
            isBoostAt != null && isBoostAt(evt.JsonTime),
            appearance);

    // Same empty division-1 sentinel boxes LightColorGroupEffect.Initialize installs, so sentinel
    // group states generate no events while still bounding real claims on each light.
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

    // Bucket sizing only steers lookup granularity; out-of-range times clamp into the edge bucket.
    // Covers wave tails exactly and step-type tails conservatively (per-element beat steps).
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
        previous = null;
        return target != null
            && light >= 0
            && light < LightCount
            && incoming.TryGetValue((target, light), out previous);
    }

    // Outer heads resolve the same timestamp-per-light node as the outgoing path, then step back to
    // that node's literal previous segment (which may be a UsePrevious owner or the start sentinel).
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

    // Brightness carries the distribution offset, while a strobing endpoint only lights when its
    // alternate phase can still output the authored strobe brightness.
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
            // Same IsAuthoredBox rule as EventGroupEffect: automatic axis lanes are editor-only
            // placement ghosts, not serialized event boxes, so they never claim lights here.
            if (box.IsAutomaticAxisLane)
            {
                continue;
            }

            // Same Convert call as EventGroupEffect, with a null-filter guard so a box whose filter
            // was stripped between cache revisions skips cleanly instead of throwing mid-rebuild.
            var indexFilter = box.IndexFilter == null
                ? null
                : IndexFilterHelper.Convert(box.IndexFilter, LightCount);
            if (indexFilter == null)
            {
                continue;
            }

            // Same empty-box rule as EventGroupEffect: eventless claimants use relative beat zero
            // rather than indexing an empty event array.
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
                // Convert already bounds elements to [0, LightCount), but the range check mirrors the
                // runtime container lookup's 0 <= id < Count guard rather than trusting that invariant.
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

                // The runtime HandleRemoveState throws on a cache miss; the data-only path tolerates
                // a group that never claimed this light (for example a stale add after a light-count
                // revision) and skips it instead of leaving the whole removal half-applied.
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

        RemoveEvents(element, prevState);
        RegenerateEvents(element, prevState, newState.LocalJsonTime);
        RegenerateEvents(element, newState, nextState.LocalJsonTime);

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
        RegenerateEvents(element, prevState, nextState.LocalJsonTime);
        groupContainer.RemoveState(currState);
    }

    private void RemoveEvents(int element, LightColorGroupStateData state)
    {
        var eventContainer = eventContainers[element];
        foreach (var evt in state.Events)
        {
            RemoveEventState(element, eventContainer, (LightColorEventStateData)evt);
        }
    }

    private void RegenerateEvents(
        int element,
        LightColorGroupStateData state,
        float maxRelativeJsonTime)
    {
        // The claim pass already converted this box's filter once; reusing it keeps each light's
        // regeneration from re-parsing the same serialized filter.
        var indexFilter = state.Box.IndexFilter == null || state.Box.Events == null
            ? null
            : state.ConvertedIndexFilter ?? IndexFilterHelper.Convert(state.Box.IndexFilter, LightCount);
        // The runtime path only reaches regeneration for states created from a valid filter (and its
        // division-1 sentinels), but the data-only path guards instead of throwing if a box's filter
        // is edited between rebuilds.
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

    // The shared relink keeps Next/Previous identical to EventGroupEffect; the bookkeeping below
    // only updates this timeline's query indexes around it.
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

        // Track the completed relink; AddGroup refreshes each distinct bound after all affected lights are updated.
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

        // The edit boundary coalesces repeated source/target changes across the claimed lights.
        TrackChange(stateToRemove, prevState, nextState);
    }

    // One union calculation per changed identity avoids multiplying construction/edit work by the light count twice.
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
                        // LitTailExtendsOutgoingRibbonToSongEnd: a lit terminal hold still produces
                        // light, so the source interval extends to the tail bound; a dark hold
                        // contributes nothing and never renders.
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

            // LitHeadRetainsTargetGroupFromMapStart: a lit pre-node fade-in reaches back to the map
            // start, so the target's retention interval must cover it even when the sentinel owns
            // the segment itself.
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

    // Sentinel bases are indexing-internal only: their links still relink for chain consistency but
    // they never represent renderable nodes, so they stay out of the change report.
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
