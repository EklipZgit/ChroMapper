using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using ZLinq;

public class GLSEventGridContainer : BeatmapObjectContainerCollection<BaseGLSEvent>
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // A collection action replaces several child events before the parent GLS group can be rebuilt safely.
    private int groupReplacementBatchDepth;
    // Alt-drag mutates a child still referenced by its parent, so the next replacement must consume an untouched pre-drag group clone.
    private BaseEventBoxGroup nextReplacementOriginalGroupData;
    // Reuse indexed transition candidates because viewport refreshes occur while dragging and scrolling.
    private readonly List<BaseLightColorBase> retainedTransitionSources = new();

    public override ObjectType ContainerType => ObjectType.GLSEvent;

    // Queue previews need the same boost lookup as finalized GLS child nodes without rediscovering the event grid.
    public bool IsBoostAt(float jsonTime) => eventGridContainer.IsBoostAt(jsonTime);

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSEvent(null, BeatmapContext.TracksDefinition, ref eventPrefab);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated += RefreshBoostDependentAppearances;
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated -= RefreshBoostDependentAppearances;
    }

    private void RefreshBoostDependentAppearances(float startJsonTime, float endJsonTime)
    {
        foreach (var pair in LoadedContainers)
        {
            if (pair.Key is not BaseLightColorBase colorEvent
                || colorEvent.JsonTime < startJsonTime
                || colorEvent.JsonTime >= endJsonTime)
            {
                continue;
            }

            var container = pair.Value as GLSEventContainer;
            // Basic events already repaint this interval; GLS child containers need the same boost lookup reapplied.
            glsEventAppearance.SetAppearance(container, true, eventGridContainer.IsBoostAt(colorEvent.JsonTime));
            glsEventAppearance.UpdateTransitionRibbon(container, eventGridContainer.IsBoostAt);
        }
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        if (groupReplacementBatchDepth == 0) ReplaceGroup(obj, "Placed a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (groupReplacementBatchDepth == 0) ReplaceGroup(obj, "Deleted a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    public void BeginGroupReplacementBatch()
    {
        // Suppress intermediate GLS group actions while a bulk edit replaces individual child events.
        groupReplacementBatchDepth++;
    }

    public void EndGroupReplacementBatch(string message)
    {
        // Publish one complete parent group so the light simulation never caches a partial bulk edit.
        if (groupReplacementBatchDepth == 0) return;
        groupReplacementBatchDepth--;
        if (groupReplacementBatchDepth == 0 && MapObjects.Count > 0) ReplaceGroup(MapObjects[0], message);
    }

    // A nested parent replacement selects the parent, so restore the affected child identities after bulk child actions finish.
    public void RebindSelectionAfterBatch(IEnumerable<BaseGLSEvent> sourceEvents)
    {
        var group = glsEventGridProvider.GroupContext;
        if (group == null || MapObjects.Count == 0)
        {
            return;
        }

        SelectionController.Deselect(group, false);
        var replacementLookup = new GLSEventReplacementLookup(MapObjects);
        foreach (var sourceEvent in sourceEvents)
        {
            if (sourceEvent.EventBoxGroupData?.CompareTo(group) != 0)
            {
                continue;
            }

            if (replacementLookup.TryTake(sourceEvent, out var replacement))
            {
                SelectionController.Select(replacement, true, false, false);
            }
        }

        SelectionController.OnSelectionChanged?.Invoke();
    }

    // Preserve the complete pre-drag parent, including the source and any destination conflict, for one replacement action.
    public void UseOriginalGroupForNextReplacement(BaseEventBoxGroup originalGroup) =>
        nextReplacementOriginalGroupData = originalGroup;

    // Rejected drags need an action-free rollback because their live parent temporarily contains the dragged child's invalid offset.
    public void RestoreRejectedDrag(BaseEventBoxGroup originalGroup)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null
            || originalGroup == null
            || liveGroup.GetType() != originalGroup.GetType())
        {
            // A rejected drag must never publish an invalid transient group merely because its original snapshot cannot be applied.
            Debug.LogError("Could not restore a rejected GLS drag because its group context was unavailable.");
            return;
        }

        // Color transition indexes own child identities, so retire them before Apply replaces every child clone in place.
        if (liveGroup is BaseLightColorEventBoxGroup oldColorGroup)
        {
            GLSEventCommon.RemoveColorTransitionGroup(oldColorGroup);
        }

        // Restore the exact live group and its open child collection without creating an undo entry for the rejected destination.
        liveGroup.Apply(originalGroup);
        if (liveGroup is BaseLightColorEventBoxGroup restoredColorGroup)
        {
            GLSEventCommon.AddColorTransitionGroup(restoredColorGroup);
        }

        nextReplacementOriginalGroupData = null;
        HandleGroupChanged(liveGroup);
    }

    // Selection deletion must publish one parent edit instead of one replacement action per selected child.
    public BeatmapObjectUpdatedAction CreateSelectionDeleteAction(IReadOnlyCollection<BaseGLSEvent> deletedEvents)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null || deletedEvents.Count == 0)
        {
            return null;
        }

        // A hash set keeps the single open-group rebuild linear when many selected inner nodes are deleted together.
        var excludedEvents = new HashSet<BaseGLSEvent>(deletedEvents);
        var newGroup = BuildGroupFromMapObjects(liveGroup, excludedEvents);
        // One parent replacement makes the entire inner GLS selection deletion one undoable action.
        return new BeatmapObjectUpdatedAction(newGroup, liveGroup, "Deleted a selection of GLS events.");
    }

    // stop it, no action for delete
    public override void RemoveConflictingObjects(
        IEnumerable<BaseGLSEvent> newObjects,
        out List<BaseGLSEvent> conflicting)
    {
        conflicting = new List<BaseGLSEvent>();

        foreach (var newObject in newObjects)
        {
            var localWindow = GetBetween(newObject.JsonTime - 0.1f, newObject.JsonTime + 0.1f);

            for (var i = 0; i < localWindow.Length; i++)
            {
                var obj = localWindow[i];
                if (obj.IsConflictingWith(newObject) && newObject != obj) conflicting.Add(obj);
            }

        }

        conflicting.ForEach(conflict => DeleteObject(conflict, false, false, triggerHandle: false));
    }

    private void ReplaceGroup(BaseObject obj, string msg)
    {
        var glsEvt = obj as BaseGLSEvent;
        var liveOriginalGroup = glsEventGridProvider.GroupContext;
        if (liveOriginalGroup == null
            || !ReferenceEquals(glsEvt.EventBoxGroupData, liveOriginalGroup))
        {
            // A retired dragged child must never replace the currently open group after undo or another group action changes context.
            // SpawnObject inserts before this callback, so remove the rejected stale child without publishing another group action.
            SilentRemoveObject(glsEvt);
            nextReplacementOriginalGroupData = null;
            return;
        }

        var originalGroupData = nextReplacementOriginalGroupData;
        nextReplacementOriginalGroupData = null;
        // Convert the authoritative child collection back into one replacement parent group.
        var newGroup = BuildGroupFromMapObjects(glsEvt.EventBoxGroupData);

        if (originalGroupData != null)
        {
            // Restore the exact live group before manager removal; the immutable clone supplies its pre-drag child data only.
            liveOriginalGroup.Apply(originalGroupData);
        }

        // Updated actions distinguish the exact live removal identity from the edited replacement needed for undo and redo.
        var action = new BeatmapObjectUpdatedAction(newGroup, liveOriginalGroup, msg);
        BeatmapActionContainer.AddAction(action, true);
    }

    // Share the open-group rebuild so single edits and bulk selection deletes preserve identical lane/conflict behavior.
    private BaseEventBoxGroup BuildGroupFromMapObjects(
        BaseEventBoxGroup sourceGroup,
        ISet<BaseGLSEvent> excludedEvents = null)
    {
        // Every inner mutation must rebuild only its open parent and preserve all authored filter lanes.
        var newGroup = BeatmapFactory.Clone(sourceGroup);
        // Preserve every authored lane when the final event is deleted so the mapper can place into them again.
        foreach (var box in newGroup.ReadOnlyBoxes) box.ClearEvents();

        // the typa shit i had to pull to amke this work
        foreach (var boxEvents in MapObjects
            .Where(e => excludedEvents == null || !excludedEvents.Contains(e))
            .Select(e =>
            {
                var newEvt = BeatmapFactory.Clone(e);
                newEvt.EventBoxGroupData = newGroup;
                newEvt.EventBoxData = newGroup.ReadOnlyBoxes[e.BoxIndex];
                newEvt.BoxIndex = e.BoxIndex;
                return newEvt;
            })
            .GroupBy(e => e.BoxIndex))
            newGroup.ReadOnlyBoxes[boxEvents.Key].SetEvents(boxEvents.ToArray());
        // co-variant deez

        // Rebuild the maintained preview ordering once at this mutation boundary so render refreshes never need to rescan the group.
        newGroup.ResortOrderedEvents();
        return newGroup;
    }

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        // Snapshot only selected child nodes owned by the retiring collection before parent replacement changes identity.
        var selectedEvents = new List<BaseGLSEvent>();
        foreach (var selectedObject in SelectionController.SelectedObjects)
        {
            if (selectedObject is BaseGLSEvent selectedEvent && MapObjects.Contains(selectedEvent))
            {
                selectedEvents.Add(selectedEvent);
            }
        }

        // A deleted outer group has no replacement context; retire every inner child and its pooled visual immediately.
        if (group == null)
        {
            Debug.Log($"[GLSGroupContext] Cleared inner GLS collection events={MapObjects.Count} selected={selectedEvents.Count}.");
            while (ObjectsWithContainers.Count > 0)
            {
                RecycleContainer(
                    ObjectsWithContainers[ObjectsWithContainers.Count - 1],
                    indexInObjectsWithContainers: ObjectsWithContainers.Count - 1);
            }

            MapObjects.Clear();
            foreach (var selectedEvent in selectedEvents)
            {
                SelectionController.Deselect(selectedEvent, false);
            }

            if (selectedEvents.Count > 0)
            {
                SelectionController.OnSelectionChanged?.Invoke();
            }

            return;
        }

        var newEvents = group.ReadOnlyBoxes.AsValueEnumerable().SelectMany(box => box.ReadOnlyEvents).ToArray();

        // Retire visuals owned by the previous parent before replacing the child-object identities.
        while (ObjectsWithContainers.Count > 0)
            RecycleContainer(ObjectsWithContainers[ObjectsWithContainers.Count - 1], indexInObjectsWithContainers: ObjectsWithContainers.Count - 1); // Clearing list from index 0 made this O(N^2) including N^2/2 position shifts.... clearing from the back to front is O(N) if we dont scan with .Remove
        MapObjects.Clear();
        MapObjects.AddRange(newEvents);
        MapObjects.Sort();
        RefreshPool();

        if (selectedEvents.Count == 0) return;

        // Queue replacement nodes by identity once so stacked duplicates rebind in O(old selections + replacements).
        var replacementLookup = new GLSEventReplacementLookup(newEvents);
        foreach (var selectedEvent in selectedEvents)
        {
            SelectionController.Deselect(selectedEvent, false);
            if (selectedEvent.EventBoxGroupData?.CompareTo(group) != 0)
                continue;

            if (!replacementLookup.TryTake(selectedEvent, out var replacement))
                continue;

            SelectionController.Select(replacement, true, false, false);
        }

        SelectionController.OnSelectionChanged?.Invoke();
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var c = con as GLSEventContainer;
        con.UpdateGridPosition();

        glsEventAppearance.SetAppearance(c, true, eventGridContainer.IsBoostAt(obj.JsonTime));
        // Render linear color transitions from this inner node to a matching transition in any GLS group.
        glsEventAppearance.UpdateTransitionRibbon(c, eventGridContainer.IsBoostAt);
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Query transition intervals crossing the boundary instead of scanning every inner GLS node.
        GLSEventCommon.GetColorTransitionSourcesAt(
            lowerBound,
            glsEventGridProvider.GroupContext,
            TrackFilterID,
            retainedTransitionSources);
        for (var sourceIndex = 0; sourceIndex < retainedTransitionSources.Count; sourceIndex++)
        {
            var source = retainedTransitionSources[sourceIndex];
            if (!LoadedContainers.ContainsKey(source))
            {
                CreateContainerFromPool(source);
            }
        }
    }

    public override void DeleteObject(
        BaseGLSEvent obj,
        bool triggersAction = true,
        bool refreshesPool = true,
        string comment = "No comment.",
        bool inCollectionOfDeletes = false,
        bool deselect = true,
        bool triggerHandle = true)
    {
        if (!TryBinarySearch(obj, out var search)) return;
        var deletedObj = MapObjects[search];
        RecycleContainer(deletedObj);
        MapObjects.RemoveAt(search);
        if (deselect) SelectionController.Deselect(deletedObj, triggersAction);
        if (refreshesPool) RefreshPool();
        if (triggerHandle) HandleObjectDelete(deletedObj, inCollectionOfDeletes);
    }
}
