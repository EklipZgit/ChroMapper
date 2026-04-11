using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class GLSEventGridContainer : BeatmapObjectContainerCollection<BaseGLSEvent>
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    public override ObjectType ContainerType => ObjectType.GLSEvent;

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSEvent(null, BeatmapContext.TrackDefinitions, ref eventPrefab);

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
        ReplaceGroup(obj, "Placed a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        ReplaceGroup(obj, "Deleted a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    // stop it, no action for delete
    public override void RemoveConflictingObjects(
        IEnumerable<BaseGLSEvent> newObjects,
        out List<BaseGLSEvent> conflicting)
    {
        conflicting = new List<BaseGLSEvent>();

        foreach (var newObject in newObjects)
        {
            Debug.Log($"Performing conflicting check at {newObject.JsonTime}");
            var localWindow = GetBetween(newObject.JsonTime - 0.1f, newObject.JsonTime + 0.1f);

            for (var i = 0; i < localWindow.Length; i++)
            {
                var obj = localWindow[i];
                if (obj.IsConflictingWith(newObject) && newObject != obj) conflicting.Add(obj);
            }
        }

        conflicting.ForEach(conflict => DeleteObject(conflict, false, false, triggerHandle: false));

        Debug.Log($"Removed {conflicting.Count} conflicting {ContainerType}s.");
    }

    private void ReplaceGroup(BaseObject obj, string msg)
    {
        var glsEvt = obj as BaseGLSEvent;
        // convert back collection and replace the group instead
        var newGroup = BeatmapFactory.Clone(glsEvt.EventBoxGroupData);
        // the typa shit i had to pull to amke this work
        foreach (var box in newGroup.ReadOnlyBoxes) box.ClearEvents();
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
        MapObjects.Clear();
        MapObjects.AddRange(group.ReadOnlyBoxes.SelectMany(box => box.ReadOnlyEvents));
        MapObjects.Sort();
        RefreshPool(true);
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var c = con as GLSEventContainer;
        con.UpdateGridPosition();

        glsEventAppearance.SetAppearance(
            c,
            true,
            eventGridContainer.AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1);
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
