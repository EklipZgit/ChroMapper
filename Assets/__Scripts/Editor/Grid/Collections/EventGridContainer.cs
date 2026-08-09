using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventGridContainer : BeatmapObjectContainerCollection<BaseEvent>, CMInput.IEventGridActions
{
    public enum PropMode
    {
        Off, Prop, Light
    }

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private GridLane gridLane;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private CountersPlusController countersPlus;

    public int EventTypeToPropagate = (int)EventTypeValue.Event1;
    public int EventTypePropagationSize;

    // Isolate boost ordering and predecessor queries from the grid's rendering and invalidation responsibilities.
    private readonly ColorBoostEventIndex boostEventIndex = new();

    public List<BaseEvent> AllBpmEvents = new();

    private readonly HashSet<BaseEvent> lightEventsWithKnownPrevNext = new();

    // Keep propagation-off label refreshes independent of the complete basic-event map size.
    private readonly BasicEventNameFilterIndex nameFilterIndex = new();

    private Dictionary<int, List<BaseEvent>> allLightEvents = new();

    public Dictionary<int, List<BaseEvent>> AllLightEvents
    {
        get => allLightEvents;
        set
        {
            allLightEvents = value;
            foreach (var p in allLightEvents)
            {
                var lightList = p.Value;

                if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
                    LinkEventsForChroma(lightList);
                else
                    LinkEventsForVanilla(lightList);
            }
        }
    }

    public Dictionary<int, BasicLightEffect> TypeToManager = new();
    private PropMode propagationEditing = PropMode.Off;

    public override ObjectType ContainerType => ObjectType.Event;

    public PropMode PropagationEditing
    {
        get => propagationEditing;
        set
        {
            propagationEditing = value;
            boxSelectionPlacement.Cancel();

            var propagationLength = 0;
            if (TypeToManager.TryGetValue(EventTypeToPropagate, out var lightingManager))
            {
                propagationLength =
                    (value == PropMode.Light
                        ? lightingManager.LaneToLightID?.Count
                        : lightingManager.LaneToLightIDs?.Count)
                    ?? 0;
            }

            labels.UpdateLabels(
                value,
                EventTypeToPropagate,
                propagationLength + 1);
            gridLane.Lane =
                value != PropMode.Off
                    ? propagationLength + 1
                    : labels.LaneCount;
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }

    public void OnToggleLightPropagation(InputAction.CallbackContext context)
    {
        if (context.performed) PropagationEditing = PropagationEditing == PropMode.Prop ? PropMode.Off : PropMode.Prop;
    }

    public void OnToggleLightIdMode(InputAction.CallbackContext context)
    {
        if (context.performed)
            PropagationEditing = PropagationEditing == PropMode.Light ? PropMode.Off : PropMode.Light;
    }

    public void OnResetRings(InputAction.CallbackContext context)
    {
        if (!context.performed || laserSpeedController.Activated) return;

        // if (descriptor.BigRingRotationManager is TrackLaneRingsRotationManager manager) manager.RotationEffect.Reset();
        //
        // if (descriptor.SmallRingRotationManager != null && descriptor.SmallRingRotationManager.RotationEffect != null)
        //     descriptor.SmallRingRotationManager.RotationEffect.Reset();
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var ids = TypeToManager.Keys.ToList();
        var id = ids.IndexOf(EventTypeToPropagate);

        EventTypeToPropagate = id == -1 ? ids.First() : ids[(int)Mathf.Repeat(id + 1, ids.Count)];
        PropagationEditing = PropagationEditing;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var ids = TypeToManager.Keys.ToList();
        var id = ids.IndexOf(EventTypeToPropagate);

        EventTypeToPropagate = id == -1 ? ids.First() : ids[(int)Mathf.Repeat(id - 1, ids.Count)];
        PropagationEditing = PropagationEditing;
    }

    public static string GetKeyForProp(PropMode mode)
    {
        if (mode == PropMode.Light) return "_lightID";

        return mode == PropMode.Prop ? "_propID" : null;
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        // Bind the map-scoped index before the environment reset asks labels to render propagation-off lanes.
        nameFilterIndex.EnsureFor(MapObjects);
        labels.NameFilterIndex = nameFilterIndex;
        TypeToManager = descriptor
            .BasicEventEffectManager.GetEffects<BasicLightEffect>()
            .ToDictionary(x => x.type, x => x.effect);
        PropagationEditing = PropMode.Off;
    }

    internal override void SubscribeToCallbacks()
    {
        // Give labels their map-scoped index before any environment callback can request a refresh.
        labels.NameFilterIndex = nameFilterIndex;
        BeatmapContext.OnEnvironmentLoaded += HandleEnvironmentLoaded;
        SpawnCallbackController.OnEventPassedThreshold += SpawnCallback;
        SpawnCallbackController.OnRecursiveEventCheckFinished += OnRecursiveCheckFinished;
        DespawnCallbackController.OnEventPassedThreshold += DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.OnEnvironmentLoaded -= HandleEnvironmentLoaded;
        SpawnCallbackController.OnEventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.OnRecursiveEventCheckFinished -= OnRecursiveCheckFinished;
        DespawnCallbackController.OnEventPassedThreshold -= DespawnCallback;
        BeatmapContext.Atsc.OnPlayToggled -= OnPlayToggle;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            // Update the shared filter counts as the collection removes an authored basic event.
            if (!nameFilterIndex.EnsureFor(MapObjects))
            {
                nameFilterIndex.Remove(e);
            }

            if (e.IsColorBoostEvent())
            {
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
                boostEventIndex.Remove(e);
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
            }
            else if (e.IsBpmEvent())
            {
                AllBpmEvents.Remove(e);
            }
            else if (BeatmapContext.TracksDefinition.GetBasicOrDefault(e.Type).Kind == BasicEventKind.Lights
                && !inCollection)
            {
                RemoveLinkedLightEvents(e);
                if (AllLightEvents.TryGetValue(e.Type, out var events)) events.Remove(e);
            }

            MarkEventToBeRelinked(e);
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        LinkAllLightEvents();
        LinkRingEvents();
        RefreshPool();
        RefreshVirtualLanes();
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        if (obj is BaseEvent e)
        {
            // Update the shared filter counts as the collection adds an authored basic event.
            if (!nameFilterIndex.EnsureFor(MapObjects))
            {
                nameFilterIndex.Add(e);
            }

            if (e.IsColorBoostEvent())
            {
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
                boostEventIndex.Add(e);
                boostEventIndex.InvalidateAppearanceRange(e.JsonTime);
            }
            else if (e.IsBpmEvent())
            {
                AllBpmEvents.Add(e);
            }
            else if (BeatmapContext.TracksDefinition.GetBasicOrDefault(e.Type).Kind == BasicEventKind.Lights
                && !inCollection)
            {
                RemoveLinkedLightEvents(e);
                LinkLightEvents(e);
                AddToAllLightEvents(e);
                lightEventsWithKnownPrevNext.Add(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override void DoPostObjectsSpawnedWorkflow()
    {
        LinkAllLightEvents();
        LinkRingEvents();
        RefreshVirtualLanes();
    }

    private void LinkLightEvents(BaseEvent e)
    {
        var previousEvent = GetPreviousEventWithSameLightIDOrDefault(e);
        if (previousEvent != null)
        {
            previousEvent.Next = e;
            if (LoadedContainers.TryGetValue(previousEvent, out var value))
                (value as EventContainer).RefreshAppearance();
        }

        var nextEvent = GetNextEventWithSameLightIDOrDefault(e);
        if (nextEvent != null) nextEvent.Prev = e;

        e.Prev = previousEvent;
        e.Next = nextEvent;
    }

    private void RemoveLinkedLightEvents(BaseEvent e)
    {
        // Update appearance of previous event
        if (e.Prev != null)
        {
            if (e.Next != null)
                (e.Prev.Next, e.Next.Prev) = (e.Next, e.Prev);
            else
                e.Prev.Next = null;

            if (LoadedContainers.TryGetValue(e.Prev, out var prevContainer))
                (prevContainer as EventContainer).RefreshAppearance();
        }
    }

    private void AddToAllLightEvents(BaseEvent e)
    {
        if (AllLightEvents.TryGetValue(e.Type, out var events))
        {
            if (e.Prev == null)
                events.Add(e);
            else
                events.Insert(events.IndexOf(e.Prev) + 1, e);
        }
        else
            AllLightEvents.Add(e.Type, new List<BaseEvent> { e });
    }

    private BaseEvent GetPreviousEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events)) return null;

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.FindLast(x => x.JsonTime < e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.FindLast(x => x.JsonTime < e.JsonTime);
    }

    private BaseEvent GetNextEventWithSameLightIDOrDefault(BaseEvent e)
    {
        if (!AllLightEvents.TryGetValue(e.Type, out var events)) return null;

        if (Settings.Instance.EmulateChromaAdvanced && Settings.Instance.LightIDTransitionSupport)
        {
            var thisLightID = e.CustomLightID?.FirstOrDefault();
            return events.Find(x => x.JsonTime > e.JsonTime && thisLightID == x.CustomLightID?.FirstOrDefault());
        }

        return events.Find(x => x.JsonTime > e.JsonTime);
    }

    // TODO: bleh, who cares about prop ID anyway
    // public override IEnumerable<BaseObject> GrabSortedObjects()
    // {
    //     var sorted = new List<BaseObject>();
    //     var grouping = LoadedObjects.GroupBy(x => x.Time);
    //     foreach (var group in grouping)
    //     {
    //         sorted.AddRange(@group.Where(x => x is BaseEvent).Cast<BaseEvent>().OrderBy(x =>
    //             x.CustomData.HasKey(x.CustomKeyPropID) ? x.CustomData[x.CustomKeyPropID].AsInt : -1)); // Sort non-light prop events before light prop events
    //     }
    //
    //     return sorted;
    // }

    private void RefreshVirtualLanes()
    {
        // Rebuild once after a map load that replaces the backing event list outside collection callbacks.
        nameFilterIndex.EnsureFor(MapObjects);
        labels.NameFilterIndex = nameFilterIndex;
        if (propagationEditing == PropMode.Off)
            PropagationEditing = PropMode.Off;
    }

    private void UpdatePropagationMode()
    {
        foreach (var con in LoadedContainers.Values)
        {
            if (con is not EventContainer e) continue;

            if (propagationEditing != PropMode.Off)
                con.SafeSetActive(e.EventData.Type == EventTypeToPropagate);
            else
                con.SafeSetActive(true);

            con.UpdateGridPosition();
        }

        if (propagationEditing == PropMode.Off) OnPlayToggle(BeatmapContext.Atsc.IsPlaying);
    }

    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            var e = objectData as BaseEvent;
            // Keep a ribbon's source container loaded until its visible destination reaches the despawn boundary.
            if (TryGetVisibleRibbonEndTime(e, out _))
                StartCoroutine(nameof(WaitForRibbonThenRecycle), e);
            else
                RecycleContainer(objectData);
        }
    }

    private IEnumerator WaitForRibbonThenRecycle(BaseEvent @event)
    {
        // Re-evaluate once when scheduled so custom gradients and linked Basic Event transitions share one lifetime rule.
        TryGetVisibleRibbonEndTime(@event, out var endTime);
        yield return new WaitUntil(() =>
            endTime < BeatmapContext.Atsc.CurrentJsonTime + DespawnCallbackController.Offset);
        RecycleContainer(@event);
    }

    private bool TryGetVisibleRibbonEndTime(BaseEvent @event, out float endTime)
    {
        endTime = 0f;
        if (!Settings.Instance.VisualizeChromaGradients || !isActiveAndEnabled)
            return false;

        // Authored Chroma gradients retain their existing duration-based lifetime.
        if (@event.CustomLightGradient != null)
        {
            endTime = @event.JsonTime + @event.CustomLightGradient.Duration;
            return true;
        }

        var nextEvent = @event.Next;
        if (BeatmapContext.TracksDefinition.GetBasicOrDefault(@event.Type).Kind != BasicEventKind.Lights
            || @event.IsFade
            || @event.IsFlash
            || nextEvent == null
            || !nextEvent.IsTransition)
        {
            return false;
        }

        // Synthesized transition ribbons end at the linked destination event for the same light-ID lane.
        endTime = nextEvent.JsonTime;
        return true;
    }

    private void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
            // Cancel every delayed ribbon recycle before rebuilding the stopped timeline pool.
            StopCoroutine(nameof(WaitForRibbonThenRecycle));
            RefreshPool();
        }
    }

    private void OnRecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        var epsilon = Mathf.Pow(10, -9);
        RefreshPool(
            BeatmapContext.Atsc.CurrentSongBpmTime + DespawnCallbackController.Offset - epsilon,
            BeatmapContext.Atsc.CurrentSongBpmTime + SpawnCallbackController.Offset + epsilon);
    }

    public override ObjectContainer CreateContainer() =>
        EventContainer.SpawnEvent(
            this,
            null,
            BeatmapContext.TracksDefinition,
            ref eventPrefab,
            ref labels);

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        base.RefreshPool(lowerBound, upperBound, forceRefresh);
        boostEventIndex.RefreshDependentAppearances(this);
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var eventContainer = con as EventContainer;
        // Rebind pooled and cloned event containers to the active environment metadata whenever they receive event data.
        eventContainer.TracksDefinition = BeatmapContext.TracksDefinition;
        eventAppearance.SetAppearance(
            eventContainer,
            true,
            IsBoostAt(obj.JsonTime));
        var e = obj as BaseEvent;
        if (PropagationEditing != PropMode.Off && e.Type != EventTypeToPropagate) con.SafeSetActive(false);
    }

    private void LinkEventsForChroma(List<BaseEvent> events)
    {
        var mostRecentEventByLightId = new Dictionary<int, BaseEvent>();

        for (var i = 0; i < events.Count; ++i)
        {
            var evt = events[i];
            var thisLightID = evt.CustomLightID?.FirstOrDefault();
            if (lightEventsWithKnownPrevNext.Add(evt))
            {
                evt.Prev = null;
                if (mostRecentEventByLightId.TryGetValue(thisLightID ?? int.MinValue, out var previousEvent))
                {
                    evt.Prev = previousEvent;
                    previousEvent.Next = evt;
                }

                evt.Next = null;
                for (var j = i + 1; j < events.Count; j++)
                {
                    if (thisLightID == events[j].CustomLightID?.FirstOrDefault())
                    {
                        events[j].Prev = evt;
                        evt.Next = events[j];
                        break;
                    }
                }
            }

            // Default is int.MinValue because there's going some mapper that will use negative lightID
            mostRecentEventByLightId[thisLightID ?? int.MinValue] = evt;
        }
    }

    private void LinkEventsForVanilla(List<BaseEvent> events)
    {
        if (events.Count == 0) return;

        if (events.Count == 1)
        {
            events[0].Prev = null;
            events[0].Next = null;
            return;
        }

        events[0].Prev = null;
        events[0].Next = events[1];

        for (var i = 1; i < events.Count - 1; i++)
        {
            events[i].Prev = events[i - 1];
            events[i].Next = events[i + 1];
        }

        events[^1].Prev = events[^2];
        events[^1].Next = null;
    }

    public void MarkEventsToBeRelinked(IEnumerable<BaseEvent> events)
    {
        foreach (var e in events) MarkEventToBeRelinked(e);
    }

    public void MarkEventToBeRelinked(BaseEvent e)
    {
        lightEventsWithKnownPrevNext.Remove(e.Prev);
        lightEventsWithKnownPrevNext.Remove(e);
        lightEventsWithKnownPrevNext.Remove(e.Next);
    }

    public void LinkAllLightEvents() =>
        AllLightEvents = MapObjects
            .Where(x => BeatmapContext.TracksDefinition.GetBasicOrDefault(x.Type).Kind == BasicEventKind.Lights)
            .GroupBy(x => x.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

    private void LinkRingEvents()
    {
        BaseEvent prevRotation = null;
        BaseEvent prevZoom = null;

        foreach (var e in MapObjects)
        {
            var components = BeatmapContext.TracksDefinition.GetBasicOrDefault(e.Type).Components;
            if (components.HasFlag(BasicEventComponent.RingRotation))
            {
                if (prevRotation != null)
                {
                    prevRotation.Next = e;
                    e.Prev = prevRotation;
                }
                else
                {
                    e.Prev = null;
                }

                prevRotation = e;
            }
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            if (components.HasFlag(BasicEventComponent.RingZoom)
                || components.HasFlag(BasicEventComponent.SmoothStepRingZoom))
            {
                if (prevZoom != null)
                {
                    prevZoom.Next = e;
                    e.Prev = prevZoom;
                }
                else
                {
                    e.Prev = null;
                }

                prevZoom = e;
            }
        }

        if (prevRotation != null) prevRotation.Next = null;
        if (prevZoom != null) prevZoom.Next = null;
    }

    public bool IsBoostAt(float jsonTime)
    {
        return boostEventIndex.IsBoostAt(jsonTime);
    }

    // Keep map-load ownership on the grid while the index owns its data representation.
    public void LoadBoostEvents(IEnumerable<BaseEvent> events)
    {
        boostEventIndex.Load(events);
    }

    public override void SilentRemoveObject(BaseObject obj)
    {
        if (obj is not BaseEvent evt || !evt.IsColorBoostEvent())
        {
            base.SilentRemoveObject(obj);
            return;
        }

        if (!TryBinarySearch(evt, out _))
        {
            return;
        }

        // Alt-drag temporarily removes the authored boost, so invalidate both its old and replacement ranges.
        boostEventIndex.InvalidateAppearanceRange(evt.JsonTime);
        base.SilentRemoveObject(evt);
        boostEventIndex.Remove(evt);
        boostEventIndex.InvalidateAppearanceRange(evt.JsonTime);
    }

    public void RefreshEventsAppearance(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events)
        {
            if (evt.Prev != null && LoadedContainers.TryGetValue(evt.Prev, out var evtPrevContainer))
                (evtPrevContainer as EventContainer).RefreshAppearance();
            if (LoadedContainers.TryGetValue(evt, out var evtContainer))
                (evtContainer as EventContainer).RefreshAppearance();
        }
    }

    public void UpdateColor(Color red, Color redBoost, Color blue, Color blueBoost, Color white, Color whiteBoost)
    {
        eventAppearance.RedColor = red;
        eventAppearance.RedBoostColor = redBoost;
        eventAppearance.BlueColor = blue;
        eventAppearance.BlueBoostColor = blueBoost;
        eventAppearance.WhiteColor = white;
        eventAppearance.WhiteBoostColor = whiteBoost;
    }
}
