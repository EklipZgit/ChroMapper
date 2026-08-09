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
    // Reuse indexed transition candidates because viewport refreshes occur while dragging and scrolling.
    private readonly List<BaseLightColorBase> retainedTransitionSources = new();

    public override ObjectType ContainerType => ObjectType.GLSEvent;

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSEvent(null, BeatmapContext.TracksDefinition, ref eventPrefab);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
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
        // convert back collection and replace the group instead
        var newGroup = BeatmapFactory.Clone(glsEvt.EventBoxGroupData);
        // Preserve every authored lane when the final event is deleted so the mapper can place into them again.
        foreach (var box in newGroup.ReadOnlyBoxes) box.ClearEvents();
        if (MapObjects.Count == 0)
        {
            Debug.Log(
                $"[GLSEventDelete] Preserving empty group id={newGroup.ID} beat={newGroup.JsonTime} " +
                $"lanes={newGroup.ReadOnlyBoxes.Count}.");
        }

        // the typa shit i had to pull to amke this work
        foreach (var boxEvents in MapObjects
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

        var action = new BeatmapObjectPlacementAction(newGroup, new[] { glsEvt.EventBoxGroupData }, msg);
        BeatmapActionContainer.AddAction(action, true);
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
